using System.Reflection;
using System.Runtime.CompilerServices;
using Tellurian.Trains.LocoNetMonitor.Slots;
using Tellurian.Trains.LocoNetMonitor.Throttles;
using Tellurian.Trains.LocoNetMonitor.Udp;

[assembly: InternalsVisibleTo("Tellurian.Trains.LocoNetMonitor.Tests")]


IHost host = Host.CreateDefaultBuilder(args)
.ConfigureServices((context, services) =>
{
    services.Configure<AppSettings>(context.Configuration.GetSection(nameof(AppSettings)));
    services.AddSingleton<SlotTable>();
    services.AddSingleton<ILocoOwnerService, CsvFileLocoOwnerService>();
    services.AddHostedService<UdpBroadcaster>();
    services.AddHostedService<UdpForwarder>();
    services.AddHostedService<SlotTableUpdater>();
    services.AddHostedService<WiThrottleServer>(); services.AddSingleton<ISerialPortGateway, SerialPortGateway>();

    WriteStartingMessage(context, services);
})
.Build();

await host.RunAsync();

static void WriteStartingMessage(HostBuilderContext context, IServiceCollection services)
{
    var provider = services.BuildServiceProvider();
    var logger = provider.GetService<ILogger<SerialPortGateway>>();
    if (logger is not null)
    {
        logger.LogInformation("Tellurian LocoNet Monitor, version {version}, environment {environment}", Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3), context.HostingEnvironment.EnvironmentName);
    }
}
