namespace Tellurian.Trains.LocoNetMonitor;
internal static class ByteExtensions
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
        var check = data[0];
        for (var i = 1; i < data.Length - 1; i++)
        {
            check ^= data[i];
        }
        return (byte)(~check);
    }
}
