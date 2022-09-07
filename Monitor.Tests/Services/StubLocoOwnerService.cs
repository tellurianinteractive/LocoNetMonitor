using Tellurian.Trains.LocoNetMonitor.Services;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Services;


internal class StubLocoOwnerService : ILocoOwnerService
{
    private readonly string? _ownerName;
    public StubLocoOwnerService(string? ownerName = null) => _ownerName = ownerName;
    public string? GetOwner(short locoAddress) => _ownerName;
}
