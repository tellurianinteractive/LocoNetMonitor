namespace Tellurian.Trains.LocoNetMonitor;

internal record Packet(byte[] Data)
{
    public bool IsComplete { get; init; } = true;
    public int Length => Data.Length;
    public override string ToString() => string.Join(',', Data.Select(p => p.ToString("X2")));
}
