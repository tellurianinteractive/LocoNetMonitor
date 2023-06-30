using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

[TestClass]
public class WiThrottleTests
{
    private static readonly IPEndPoint TesterEndPoint = new(LocalAddress, 12090);
    private static readonly WiThrottleServerSettings Settings = new(12090, 50, 30);
    private static readonly WiFredSimulatorSettings SimulatorSettings = new(TesterEndPoint,  TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10));

    private readonly TestTimeProvider TimeProvider = new() { CurrentTime = DateTimeOffset.Parse("2023-07-01 12:00:00")};
    private readonly TestSlotTable SlotTable = new();

    private readonly ILoggerFactory LogFactory = LoggerFactory.Create(f => f.AddConsole().SetMinimumLevel(LogLevel.Debug));
    private readonly IOptions<AppSettings> AppSettings = Options.Create(new AppSettings() { WiThrottleServer = Settings });

    private readonly CancellationTokenSource CancellationTokenSource = new();
    private CancellationToken Token => CancellationTokenSource.Token;

    private Task ServerTask = Task.CompletedTask;
    private async Task<(WiThrottleTestServer server, WiFredSimulator simulator)> TestInitialize()
    {
        var server = new WiThrottleTestServer(Settings);
        var simulator = new WiFredSimulator(SimulatorSettings, TimeProvider, LogFactory.CreateLogger<WiFredSimulator>());
        ServerTask = await Task.Factory.StartNew(() => server.StartAsync(Token));
        await Task.Delay(200);
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
        CancellationTokenSource.Cancel();
        await Task.Delay(1000);

    }

    [TestMethod]
    public async Task ThrottleConnects()
    {
        var (server, simulator) = await TestInitialize();
        await Task.Delay(1000);
        Assert.AreEqual(1, server.Server.Throttles.Count());
        await TestCleanUp(server, simulator);
    }

    [TestMethod]
    public async Task ReceivesInitMessageFromServer()
    {
        var (server, simulator) = await TestInitialize();
        await Task.Delay(100);
        Assert.AreEqual(4, simulator.ReceivedMessages.Count());
        await TestCleanUp(server, simulator);
    }
    [TestMethod]
    public async Task ReceivesHeartbeat()
    {
        var (server, simulator) = await TestInitialize();
        await Task.Delay(10000);
        Assert.AreEqual(4 ,simulator.ReceivedMessages.Count());
        await TestCleanUp(server, simulator);
    }


    private static IPAddress LocalAddress =>
         Dns.GetHostAddresses(Dns.GetHostName()).First(a => a.GetAddressBytes()[0] == 192);


}
