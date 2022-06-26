using System.Reflection;
using Tellurian.Trains.LocoNetMonitor;

IHost host = Host.CreateDefaultBuilder(args)
.ConfigureServices((context, services) =>
{
    services.Configure<AppSettings>(context.Configuration.GetSection(nameof(AppSettings)));
    services.AddHostedService<LocoNetBroadcaster>();
    services.AddHostedService<LocoNetMonitor>();
    services.AddHostedService<SlotTableUpdater>();
    services.AddSingleton<LocoNetInterface>();
    services.AddSingleton<SlotTable>();
    services.AddSingleton<ILocoOwnerService, CsvFileLocoOwnerService>();
    WriteStartingMessage(context,services);
})
.Build();

await host.RunAsync();

static void WriteStartingMessage(HostBuilderContext context,IServiceCollection services)
{
    var provider = services.BuildServiceProvider();
    var logger = provider.GetService<ILogger<LocoNetInterface>>();
    if (logger is not null)
    {
        logger.LogInformation("Tellurian LocoNet Monitor, version {version}, environment {environment}", Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(3), context.HostingEnvironment.EnvironmentName);
    }
}