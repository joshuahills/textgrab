# TextGrab

A tiny background utility: press a hotkey, drag a rectangle over anything on screen (a video, a
locked PDF, a remote desktop, an error dialog), and the text inside it lands on your clipboard.

Default hotkey: **Ctrl+Shift+X**. Right-click or Esc cancels.

## Install

Grab the latest from [Releases](https://github.com/joshuahills/textgrab/releases):

- **TextGrab-Setup-x.y.z.exe** (recommended): per-user install, no admin rights, bundles the .NET
  runtime, optional "start when I sign in". Upgrades in place.
- **TextGrab-x.y.z-portable-win-x64.zip**: a single exe, nothing to install. Needs the
  [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

The binaries are not code-signed, so SmartScreen may warn on first run. Choose "More info", then "Run anyway".

## Why it feels instant

- The overlay window and the OCR engine are created and warmed up when the app starts, so a
  hotkey press only has to BitBlt the desktop and call ShowWindow.
- The screenshot is wrapped as a GDI+ bitmap without copying, and repaints are clipped to the
  region that changed while you drag.
- The overlay hides the moment you release the mouse; OCR and clipboard happen after.

## Architecture

```
src/
  TextGrab.Core          net10.0            Pipeline + abstractions. No platform code.
  TextGrab.Ocr.Windows   net10.0-windows    IOcrEngine backed by Windows.Media.Ocr.
  TextGrab.Windows       net10.0-windows    Tray host: hotkey, capture, overlay, clipboard.
tests/
  TextGrab.Tests                            xunit, including real OCR tests on rendered text.
```

`TextGrab.Core` owns the whole flow (`GrabPipeline`) and defines the seams the platform fills in:

| Interface           | Windows implementation      | Notes for other platforms                      |
|---------------------|-----------------------------|------------------------------------------------|
| `IScreenCapturer`   | `GdiScreenCapturer`         | X11/Wayland portal, CoreGraphics                |
| `IRegionSelector`   | `OverlayRegionSelector`     | Any toolkit that can show a full-screen window  |
| `IClipboard`        | `WindowsClipboard`          | xclip/wl-copy, NSPasteboard                     |
| `IHotkeyService`    | `Win32HotkeyService`        | XGrabKey, GlobalShortcuts portal, Carbon        |
| `INotifier`         | `FlashNotifier`             | libnotify, NSUserNotification                   |
| `IOcrEngine`        | `WindowsOcrEngine`          | See below                                       |

Images move through the pipeline as `RawImage` (BGRA32 byte buffer), so nothing in Core or in an
OCR engine depends on System.Drawing or WinRT.

### Adding an OCR engine

1. Implement `IOcrEngine` (and `IOcrEngineFactory`) in a new project, e.g. `TextGrab.Ocr.Onnx`.
   Do model loading in `WarmUpAsync`; return lines with bounding boxes in `RecognizeAsync`.
2. Register the factory in `Program.cs`.
3. Set `"ocrEngine": "your-name"` in the settings file.

Candidates: a small ONNX model (PaddleOCR / RapidOCR), Tesseract, or a vision LLM for hard cases.

## Settings

`%APPDATA%\TextGrab\settings.json` (created on first run; "Open settings file" in the tray menu).

```json
{
  "hotkey": "Ctrl+Shift+X",
  "ocrEngine": "windows",
  "minOcrHeight": 64,
  "maxUpscale": 3,
  "joinLines": false,
  "showConfirmation": true
}
```

- `minOcrHeight` / `maxUpscale`: selections shorter than this are upscaled before OCR, which
  helps a lot with small subtitles.
- `joinLines`: copy as one line instead of preserving line breaks.

## Build

```
dotnet build
dotnet test
dotnet publish src/TextGrab.Windows -c Release
```

Publish produces a single-file `TextGrab.exe` (needs the .NET 10 Desktop Runtime) under
`src/TextGrab.Windows/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`.
Tick "Start with Windows" in the tray menu to run it at login.

Requires Windows 10 1809+ with an OCR-capable language pack installed (English is by default).

## Releasing

Push a tag and the `release` workflow builds the installer and portable zip, computes checksums,
and publishes a GitHub release with generated notes:

```
git tag v0.2.0
git push origin v0.2.0
```

To build the installer locally, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and run:

```
dotnet publish src/TextGrab.Windows -c Release -p:SelfContained=true -p:PublishSingleFile=false -o artifacts/installer-payload
ISCC.exe /DAppVersion=0.2.0 /DPublishDir=..rtifacts\installer-payload installer\TextGrab.iss
```
