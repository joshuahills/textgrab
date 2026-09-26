namespace TextGrab.Platform;

public interface IClipboard
{
    Task SetTextAsync(string text, CancellationToken cancellationToken = default);
}
