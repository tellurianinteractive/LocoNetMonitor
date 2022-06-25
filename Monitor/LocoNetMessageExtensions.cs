namespace Tellurian.Trains.LocoNetMonitor;
internal static class LocoNetMessageExtensions
{
    public static string ToHex(this byte[] data) => string.Join(',', data.Select(b => b.ToString("X2")));

    public static bool IsLocoSlot(this byte slot) => slot > 0 && slot < 120;

    public static byte[] AppendChecksum(this byte[] dataWithoutChecksum)
    {
        if (dataWithoutChecksum is null) return Array.Empty<byte>();
        var length = dataWithoutChecksum.Length;
        var result = new byte[length + 1];
        Array.Copy(dataWithoutChecksum, 0, result, 0, length);
        result[length] = Checksum(result);
        return result;
    }
    private static byte Checksum(byte[] data)
    {
        if (data is null || data.Length == 0) return 0;
        var check = 0;
        for (var i = 0; i < data.Length; i++)
        {
            check ^= data[i];
        }
        return (byte)(~check);
    }

    public static bool IsValidMessage(this byte[] message)
    {
        if (message is null || message.Length == 0) return false;
        return message[0] switch
        {
            (>= 0x80) and (<= 0x8F) when message.Length == 2 && message[1] == Checksum(message[..1].ToArray()) => true,
            (>= 0xA0) and (<= 0xBF) when message.Length == 4 && message[3] == Checksum(message[..3].ToArray()) => true,
            (>= 0xC0) and (<= 0xDF) when message.Length == 6 && message[5] == Checksum(message[..5].ToArray()) => true,
            (>= 0xE0) and (<= 0xFF) when message.Length == message[1] && message[message[1] - 1] == Checksum(message[..(message[1] - 1)]) => true,
            _ => false
        };
    }
}
