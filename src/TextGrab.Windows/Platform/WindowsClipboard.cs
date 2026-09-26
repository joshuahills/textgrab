using TextGrab.Platform;

namespace TextGrab.Windows.Platform;

/// <summary>Clipboard access marshalled onto the STA UI thread, with retries for when another app holds the clipboard.</summary>
public sealed class WindowsClipboard(SynchronizationContext ui) : IClipboard
{
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ui.Post(_ =>
        {
            try
            {
                Clipboard.SetDataObject(text, copy: true, retryTimes: 8, retryDelay: 40);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }
}
