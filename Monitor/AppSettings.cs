namespace Tellurian.Trains.LocoNetMonitor;

public record AppSettings
{
    public LocoNetSettings LocoNet { get; init; } = new LocoNetSettings("COM1", 9600, 10000, 10);

    public UdpSettings Udp { get; init; } = new UdpSettings("192.168.0.255", 34000, 34001);
    public SlotTableSettings SlotTable { get; init; } = new(false);
    public CsvFileLocoAddressOwnerServiceSettings? CsvFileLocoAddressOwnerService { get; init; }
}

public record LocoNetSettings(string Port, int BaudRate, int ReadTimeout, int MinWriteInterval);
public record UdpSettings(string BroadcastIPAddress, int BroadcastPort, int SendPort);

public record CsvFileLocoAddressOwnerServiceSettings() { public string? LocoOwnersListCsvFilePath { get; init; } }

public record SlotTableSettings(bool BlockDrivingForUnassignedAdresses);