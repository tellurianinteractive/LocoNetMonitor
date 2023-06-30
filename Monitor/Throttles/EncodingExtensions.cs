using System.Text;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;

public static class EncodingExtensions
{
    public static byte[] AsBytes(this string? text, char? endOfLine = null) =>
        string.IsNullOrWhiteSpace(text) ? Array.Empty<byte>() : 
        endOfLine.HasValue ? Encoding.UTF8.GetBytes(text + endOfLine) :
        Encoding.UTF8.GetBytes(text);

}
