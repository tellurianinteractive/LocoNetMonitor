namespace Tellurian.Trains.LocoNetMonitor;
internal static class LocoNetMessageSplitter
{
    public static IEnumerable<Packet> Split(this byte[] data)
    {
        if (data is null || data.Length == 0) return Enumerable.Empty<Packet>();
        var result = new List<Packet>();
        var s = data.AsSpan();
        var i = 0;
        while (i < s.Length)
        {
            var packet = s[i] switch
            {
                (>= 0x80) and (<= 0x9F) when s.Length - i >= 2 => new Packet(s.Slice(i, 2).ToArray()),
                (>= 0xA0) and (<= 0xBF) when s.Length - i >= 4 => new Packet(s.Slice(i, 4).ToArray()),
                (>= 0xC0) and (<= 0xDF) when s.Length - i >= 6 => new Packet(s.Slice(i, 6).ToArray()),
                (>= 0xE0) and (<= 0xFF) when s.Length - i >= s[i + 1] => new Packet(s.Slice(i, s[i + 1]).ToArray()),
                _ => new Packet(s[i..].ToArray()) { IsComplete = false },
            };
            if (packet is not null)
            {
                result.Add(packet);
                i += packet.Length;
            }
        }
        return result;
    }



}
