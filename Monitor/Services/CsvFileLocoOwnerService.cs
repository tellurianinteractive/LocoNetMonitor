namespace Tellurian.Trains.LocoNetMonitor.Services;

public sealed class CsvFileLocoOwnerService : ILocoOwnerService
{
    private readonly string? Path;
   private readonly ILogger Logger;
    private Owner[] Owners = Array.Empty<Owner>();
    private DateTime? LastRead = null;
    public CsvFileLocoOwnerService(IOptions<AppSettings> options, ILogger<CsvFileLocoOwnerService> logger)
    {
        Logger = logger;
        var fullPathToCsvFile = options.Value.CsvFileLocoAddressOwnerService?.LocoOwnersListCsvFilePath;
        if (fullPathToCsvFile is null) return;
        if (!File.Exists(fullPathToCsvFile)) throw new FileNotFoundException(fullPathToCsvFile);
        Path = fullPathToCsvFile;
    }
    public string? GetOwner(short locoAddress)
    {
        var lastModified = File.GetLastWriteTime(Path!);
        if (LastRead is null || lastModified > LastRead)
        {
            Owners = UpdateFromFile();
        }
        var owner = Owners.FirstOrDefault(o => o.Adresses.Contains(locoAddress));
        if (owner is null) {
            Logger.LogDebug("No owner found for loco address {address}", locoAddress);
            return null; }
        return owner.Name;
    }

    private Owner[] UpdateFromFile()
    {
        var owners = new List<Owner>(100);
        var lines = File.ReadAllText(Path!).Split(Environment.NewLine);
        foreach (var line in lines)
        {
            var items = line.Split(';');
            if (items.Length == 2 && items[1].TryParseLocoAdresses(out var adresses))
            {
                owners.Add(new Owner(items[0], adresses));
            }
        }
    
        if (IsAllAddressesUnique(owners))
        {
            LastRead = DateTime.Now;
            Logger.LogInformation("Address reservations read from {file} at {time}.", Path, LastRead.Value.ToString("g"));
            return owners.ToArray();
        }
        return Owners;
    }

    private bool IsAllAddressesUnique(IEnumerable<Owner> owners)
{
    var result = true;
    var x = owners.SelectMany(o => o.Adresses).ToArray();
    var addresses = x.GroupBy(g => g);
    foreach (var address in addresses)
    {
        if (address.Count() > 1)
        {
            result = false;
            Logger.LogWarning("Duplicate of loco address {address} for {owners}.", address.Key, string.Join(',', owners.Where(o=> o.Adresses.Contains(address.Key)).Select(o => o.Name)));
        }
    }
    return result;
}


private record Owner(string Name, int[] Adresses);

}


