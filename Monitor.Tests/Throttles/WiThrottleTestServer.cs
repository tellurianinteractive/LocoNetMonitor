using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tellurian.Trains.LocoNetMonitor.Services;
using Tellurian.Trains.LocoNetMonitor.Slots;
using Tellurian.Trains.LocoNetMonitor.Tests.LocoNet;
using Tellurian.Trains.LocoNetMonitor.Tests.Services;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;
internal class WiThrottleTestServer
{
    public WiThrottleTestServer(WiThrottleServerSettings serverSettings)
    {
        Server = Create(serverSettings);
    }

    public WiThrottleServer Server { get; }

    public static WiThrottleServer Create(WiThrottleServerSettings serverSettings)
    {
        var appSettings = new AppSettings() { WiThrottleServer = serverSettings };
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ILocoOwnerService, StubLocoOwnerService>();
        services.AddSingleton<ISerialPortGateway, TestGateway>();
        var serviceProvider = services.BuildServiceProvider();
        return new WiThrottleServer(Options.Create(appSettings), new TestSlotTable(), new SystemTimeProvider(), serviceProvider.GetRequiredService<ILogger<WiThrottleServer>>());

    }

    public Task StartAsync(CancellationToken cancellationToken) => Server.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Server.StopAsync(cancellationToken);
}
