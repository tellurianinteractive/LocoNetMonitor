using Tellurian.Trains.LocoNetMonitor.Services;
using Loco = Tellurian.Trains.LocoNetMonitor.Services.Loco;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Services;


internal class StubLocoOwnerService : ILocoOwnerService
{
    private readonly string? _ownerName;
    public StubLocoOwnerService(string? ownerName = null) => _ownerName = ownerName;
    public Loco? GetLoco(short locoAddress) => new(locoAddress, _ownerName ?? "Unknown");
}
