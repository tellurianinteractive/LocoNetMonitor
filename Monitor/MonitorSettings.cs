namespace Tellurian.Trains.LocoNetMonitor;

public record MonitorSettings
{
    public string? LocoOwnersListCsvFilePath { get; init; }
    public LocoNet LocoNet { get; set; } = new LocoNet("COM1", 9600, 10000);
    public Udp Udp { get; set; } = new Udp("192.168.0.255", 34000, 34001);
}

public record LocoNet(string Port, int BaudRate, int ReadTimeout)
{
    public bool BlockDrivingForUnassignedAdresses { get; init; }
};
public record Udp(string BroadcastIPAddress, int BroadcastPort, int SendPort);
