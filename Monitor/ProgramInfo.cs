using System.Reflection;

namespace Tellurian.Trains.LocoNetMonitor;

public static class ProgramInfo
{
    public static Version? Version => Assembly.GetCallingAssembly().GetName().Version;
}
