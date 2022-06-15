namespace Tellurian.Trains.LocoNetMonitor;

internal record Packet(byte[] Data)
{
    public bool IsComplete { get; init; } = true;
    public int Length => Data.Length;
    public override string ToString() => Data.ToHex();
}
