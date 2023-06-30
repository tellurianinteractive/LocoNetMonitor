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
        services.Configure<IOptions<AppSettings>>(x => Options.Create(appSettings));
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ITimeProvider, TestTimeProvider>();
        services.AddSingleton<ILocoOwnerService, StubLocoOwnerService>();
        services.AddSingleton<ISerialPortGateway, TestGateway>();
        services.AddSingleton<ISlotTable,TestSlotTable>();
        services.AddSingleton<WiThrottleServer>();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetService<WiThrottleServer>() ?? throw new ArgumentNullException(nameof(Server));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Server.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Server.StopAsync(cancellationToken);
}
