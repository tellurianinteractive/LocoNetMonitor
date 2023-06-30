namespace Tellurian.Trains.LocoNetMonitor.Tests;

internal class TestTimeProvider : ITimeProvider
{
    public DateTimeOffset CurrentTime { get; set; }


    public DateTimeOffset UtcNow => CurrentTime;

    public void Step(TimeSpan stepTime) { CurrentTime += stepTime; }
}
