namespace Tellurian.Trains.LocoNetMonitor;
public interface ILocoOwnerService
{
    string? GetOwner(short locoAddress);
}
