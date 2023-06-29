using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using Tellurian.Trains.LocoNetMonitor.Services;
using Tellurian.Trains.LocoNetMonitor.Slots;
using Tellurian.Trains.LocoNetMonitor.Tests.LocoNet;
using Tellurian.Trains.LocoNetMonitor.Tests.Services;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

[TestClass]
public class WiThrottleServerTests
{
    WiThrottleTestServer? Target { get; set; }

    static readonly AppSettings TestSettings = new();

    static WiThrottleServer? Create()
    {
        var services = new ServiceCollection();
        services.Configure<IOptions<AppSettings>>(x => Options.Create(TestSettings));
        services.AddLogging();
        services.AddSingleton<ILocoOwnerService, StubLocoOwnerService>();
        services.AddSingleton<ISerialPortGateway, TestGateway>();
        services.AddSingleton<SlotTable>();
        services.AddSingleton<WiThrottleServer>();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetService<WiThrottleServer>();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        Target = new(TestSettings.WiThrottleServer);
    }

    [TestMethod]
    public async Task StartsAndStops()
    {
        Assert.IsNotNull(Target);
        await Target.StartAsync(CancellationToken.None);
        await Task.Delay(1000);
        await SimulateThrottleConnect();
        await Target.StopAsync(CancellationToken.None);
    }

    static async Task SimulateThrottleConnect()
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Parse("192.168.1.2"), TestSettings.WiThrottleServer.PortNumber);
        if (tcpClient.Connected)
        {
            using var stream = tcpClient.GetStream();
            await stream.WriteAsync("NTest".AsBytes());
        }
        else
        {
            Assert.Fail("Not connected.");
        }
        await Task.Delay(1000);
        tcpClient.Close();
    }
}
