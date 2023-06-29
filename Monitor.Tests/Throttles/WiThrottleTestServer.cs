using Microsoft.Extensions.DependencyInjection;
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

    private WiThrottleServer Server { get; }

    static WiThrottleServer Create(WiThrottleServerSettings serverSettings)
    {
        var appSettings = new AppSettings() { WiThrottleServer = serverSettings };
        var services = new ServiceCollection();
        services.Configure<IOptions<AppSettings>>(x => Options.Create(appSettings));
        services.AddLogging();
        services.AddSingleton<ILocoOwnerService, StubLocoOwnerService>();
        services.AddSingleton<ISerialPortGateway, TestGateway>();
        services.AddSingleton<SlotTable>();
        services.AddSingleton<WiThrottleServer>();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetService<WiThrottleServer>() ?? throw new ArgumentNullException(nameof(Server));
    }

    public async Task StartAsync(CancellationToken cancellationToken) { await Server.StartAsync(cancellationToken); }
    public async Task StopAsync(CancellationToken cancellationToken) { await Server.StopAsync(cancellationToken); }
}
