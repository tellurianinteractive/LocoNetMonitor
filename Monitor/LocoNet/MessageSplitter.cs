namespace Tellurian.Trains.LocoNetMonitor.LocoNet;

/// <summary>
/// A read operation from a buffered stream of bytes, for example a serial port, is not guaranteed
/// to start with a LocoNet Operations Code. Likewise, further reads is not guaranteed to
/// read single LocoNet messages, but just a buffer of bytes that needs to be chunked up in 
/// LocoNet messages. The methods in this class helps to do that.
/// </summary>
internal static class MessageSplitter
{

    /// <summary>
    /// This method is intended to be called only after a first read from a data stream, 
    /// to find the first LocoNet Operation Code.
    /// </summary>
    /// <param name="data">The first data read from the stream.</param>
    /// <returns>Position of first LocoNet Operation Code; or -1 if no one is found.</returns>
    /// <remarks>This method should be called repeatedly until the first LocoNet operation code is found.</remarks>
    public static int IndexOfFirstOperationCode(this byte[] data)
    {
        for (var i = 0; i < data.Length; i++) if (data[i] > 0x80 && data[i] <= 0xFF) return i;
        return -1;
    }

    /// <summary>
    /// Splits a byte sequence into one or more <see cref="Packet"/>.
    ///    /// </summary>
    /// <param name="data"></param>
    /// <returns>A sequence of zero, one or several <see cref="Packet"/>.</returns>
    /// <remarks>This method assumes that the first byte is a LocoNet Operation Code.</remarks>
    public static IEnumerable<Packet> AsPackets(this byte[] data)
    {
        if (data.Length == 0) return Enumerable.Empty<Packet>();
        var result = new List<Packet>();
        var s = data.AsSpan();
        var i = 0;
        while (i < data.Length)
        {
            var packet = data[i] switch
            {
                >= 0x80 and <= 0x9F when data.Length - i >= 2 => new Packet(s.Slice(i, 2).ToArray()),
                >= 0xA0 and <= 0xBF when data.Length - i >= 4 => new Packet(s.Slice(i, 4).ToArray()),
                >= 0xC0 and <= 0xDF when data.Length - i >= 6 => new Packet(s.Slice(i, 6).ToArray()),
                >= 0xE0 and <= 0xFF when data.Length - i >= data[i + 1] => new Packet(s.Slice(i, data[i + 1]).ToArray()),
                _ => new Packet(data[i..].ToArray()) { IsComplete = false },
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
