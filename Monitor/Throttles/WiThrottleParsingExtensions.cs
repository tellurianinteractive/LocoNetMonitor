using System.Diagnostics.CodeAnalysis;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;
public static class WiThrottleParsingExtensions
{    public static Command? AsEntry(this string line)
    {
        if (Command.TryParse(line, out var entry))
            return entry;
        return null;
    }

    public static char AddressPrefix(this int address) => address <= 127 ? 'S' : 'L';

}





public static class WiThrottleLocoNetExtensions
{
    public static string? AsWiThrottleMessage(this byte[] locoNetMessage)
    {
        if (locoNetMessage is null || locoNetMessage.Length == 0) return null;
        return locoNetMessage[0] switch
        {

            _ => null,
        };
    }

}
