using System.Net;

namespace Tellurian.Trains.LocoNetMonitor;

public record AppSettings
{
    public LocoNetSettings LocoNet { get; init; } = new LocoNetSettings("COM1", 57600, 10000, 10);
    public UdpBroadcasterSettings UdpBroadcaster { get; init; } = new UdpBroadcasterSettings("239.1.1.255", 34010);
    public UdpForwarderSettings UdpForwarder { get; init; } = new UdpForwarderSettings(34011);
    public SlotTableSettings SlotTable { get; init; } = new(false, 34012);
    public CsvFileLocoAddressOwnerServiceSettings? CsvFileLocoAddressOwnerService { get; init; }
    public WiThrottleServerSettings WiThrottleServer { get; init; } = new(12090, 50, 20);
    public string LoggingPath { get; init; } = "C:\\Temp\\";
}

public record LocoNetSettings(string Port, int BaudRate, int ReadTimeout, int MinWriteInterval);
public record UdpBroadcasterSettings(string MulticastIPAddress, int LocalPortNumber);
public record UdpForwarderSettings(int LocalPortNumber);
public record CsvFileLocoAddressOwnerServiceSettings() { public string? LocoOwnersListCsvFilePath { get; init; } }
public record SlotTableSettings(bool BlockDrivingForUnassignedAdresses, int LocalPortNumber);

public record WiThrottleServerSettings(int PortNumber, int Backlog, int Timeout)
{
    public WiThrottleSettings[] Throttles { get; set; } = Array.Empty<WiThrottleSettings>();
}

public record WiThrottleSettings(string Id);

public static class AppSettingsExtensions
{
    public static IPAddress MulticastIPAddress(this AppSettings appSettings) => IPAddress.Parse(appSettings.UdpBroadcaster.MulticastIPAddress);
}