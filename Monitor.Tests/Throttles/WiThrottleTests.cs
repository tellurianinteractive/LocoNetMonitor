using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

[TestClass]
public class WiThrottleTests
{
    private static readonly IPEndPoint ThrottleEndPoint = new(IPAddress.Loopback, 4001);
    private static readonly IPEndPoint TesterEndPoint = new(IPAddress.Loopback, 4000);

    private readonly TcpClient Throttle = new(ThrottleEndPoint);

    private readonly TestSlotTable SlotTable = new();
    private readonly WiThrottleServerSettings Settings = new (12090, 50, 60);

    [TestMethod]
    public async Task Test()
    {
        var listener = new TcpListener(TesterEndPoint);
        listener.Start();
        Throttle.Connect(TesterEndPoint);
        var client = await listener.AcceptTcpClientAsync();
        using var throttle = new Throttle(client, Settings, SlotTable, NullLogger.Instance);
        Assert.IsNotNull(throttle);
        throttle.Dispose();
        listener.Stop();
    }

    
}
