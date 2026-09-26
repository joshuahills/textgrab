namespace TextGrab.Ocr;

/// <summary>Describes one available engine so the host can list and construct it without hard references.</summary>
public interface IOcrEngineFactory
{
    string Name { get; }
    string DisplayName { get; }
    /// <summary>False when the backend cannot run here (missing OS feature, missing model file, etc.).</summary>
    bool IsAvailable { get; }
    IOcrEngine Create();
}

public sealed class OcrEngineRegistry
{
    private readonly Dictionary<string, IOcrEngineFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    public OcrEngineRegistry Register(IOcrEngineFactory factory)
    {
        _factories[factory.Name] = factory;
        return this;
    }

    public IEnumerable<IOcrEngineFactory> All => _factories.Values;

    public IOcrEngine Create(string preferredName)
    {
        if (_factories.TryGetValue(preferredName, out var f) && f.IsAvailable) return f.Create();
        var fallback = _factories.Values.FirstOrDefault(x => x.IsAvailable)
            ?? throw new InvalidOperationException("No OCR engine is available on this system.");
        return fallback.Create();
    }
}
