namespace WindowsPrinter.Models;

public sealed class PrinterInfo
{
    public required string Name { get; init; }

    public bool IsDefault { get; init; }

    public string DiscoverySource { get; init; } = "Unknown";

    public string DisplayName => IsDefault ? $"{Name} (默认)" : Name;

    public override string ToString() => DisplayName;
}
