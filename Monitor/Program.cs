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

})
.Build();

await host.RunAsync();
