using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

[TestClass]
public class WiThrottleTests
{
    private readonly TestTimeProvider TimeProvider = new() { CurrentTime = DateTimeOffset.Parse("2023-07-01 12:00:00")};

    private readonly ILoggerFactory LogFactory = LoggerFactory.Create(f => f.AddConsole().SetMinimumLevel(LogLevel.Debug));

    private readonly CancellationTokenSource CancellationTokenSource = new();
    private CancellationToken Token => CancellationTokenSource.Token;

    private Task ServerTask = Task.CompletedTask;
    private async Task<(WiThrottleTestServer server, WiFredSimulator simulator)> TestInitialize(int portNumber, int hearbeatTimeoutSeconds = 30)
    {
        var serverSettings = new WiThrottleServerSettings(portNumber, 50, hearbeatTimeoutSeconds);
        var simulatorSettings = new WiFredSimulatorSettings(new (LocalAddress, portNumber), TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10));
        var server = new WiThrottleTestServer(serverSettings);
        var simulator = new WiFredSimulator(simulatorSettings, TimeProvider, LogFactory.CreateLogger<WiFredSimulator>());
        await server.StartAsync(Token);
        await Task.Delay(500);
        await simulator.Initialize();
        await Task.Delay(100);
        return (server, simulator);
    }

    private async Task TestCleanUp(WiThrottleTestServer server, WiFredSimulator simulator)
    {
        await Task.Delay(100);
        await simulator.Stop();
        simulator.Dispose();
        await server.StopAsync(Token);
        await Task.Delay(100);
        CancellationTokenSource.Cancel();
        await Task.Delay(2000);
    }

    [TestMethod]
    public async Task ThrottleConnects()
    {
        var (server, simulator) = await TestInitialize(12091);
        await Task.Delay(1000);
        Assert.AreEqual(1, server.Server.Throttles.Count());
        await TestCleanUp(server, simulator);
    }

    [TestMethod]
    public async Task ReceivesInitMessageFromServer()
    {
        var (server, simulator) = await TestInitialize(12092);
        await Task.Delay(1000);
        Assert.AreEqual(4, simulator.ReceivedMessages.Count());
        await TestCleanUp(server, simulator);
    }

    [TestMethod]
    public async Task ReceivesHeartbeat()
    {
        var (server, simulator) = await TestInitialize(12093, 2);
        await Task.Delay(5000);
        Assert.AreEqual(2, simulator.ReceivedMessages.Where(m => m=="*").Count());
        await TestCleanUp(server, simulator);
    }


    private static IPAddress LocalAddress =>
         Dns.GetHostAddresses(Dns.GetHostName()).First(a => a.GetAddressBytes()[0] == 192);


}
