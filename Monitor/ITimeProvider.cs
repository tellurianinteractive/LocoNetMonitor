namespace Tellurian.Trains.LocoNetMonitor;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
