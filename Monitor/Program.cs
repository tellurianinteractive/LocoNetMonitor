using Tellurian.Trains.LocoNetMonitor;

IHost host = Host.CreateDefaultBuilder(args)
.ConfigureServices((context, services) =>
{
    services.Configure<MonitorSettings>(context.Configuration.GetSection(nameof(MonitorSettings)));
    services.AddHostedService<Broadcaster>();
    services.AddHostedService<SlotTableUpdater>();
    services.AddSingleton<SlotTable>();
    services.AddSingleton<ILocoOwnerService, CsvFileLocoOwnerService>();

})
.Build();

await host.RunAsync();
