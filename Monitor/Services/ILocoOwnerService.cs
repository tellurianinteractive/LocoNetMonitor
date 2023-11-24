namespace Tellurian.Trains.LocoNetMonitor.Services;
public interface ILocoOwnerService
{
    Loco? GetLoco(short locoAddress);
}



public record Loco(short Address, string OwnerName)
{
    public string? Description { get; init; }
    public override string ToString() =>
        Description?.Length > 0 == true ?
        $"{Description} with {Address} owned by {OwnerName}" :
        $"{Address} owned by {OwnerName}";
}
