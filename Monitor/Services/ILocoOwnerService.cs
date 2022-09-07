namespace Tellurian.Trains.LocoNetMonitor.Services;
public interface ILocoOwnerService
{
    string? GetOwner(short locoAddress);
}
