using Microsoft.Extensions.Options;

namespace Tellurian.Trains.LocoNetMonitor;
public interface ILocoOwnerService
{
    string? GetOwner(short locoAddress);
}

public sealed class CsvFileLocoOwnerService : ILocoOwnerService
{
    private readonly string Path;
    public CsvFileLocoOwnerService(IOptions<AppSettings> options)
    {
        var fullPathToCsvFile = options.Value.LocoOwnersListCsvFilePath;
        if (!File.Exists(fullPathToCsvFile)) throw new FileNotFoundException(fullPathToCsvFile);
        Path = fullPathToCsvFile;
    }
    public string? GetOwner(short locoAddress)
    {
        var lines = File.ReadAllText(Path).Split(Environment.NewLine);
        var address = $"{locoAddress};";
        foreach (var line in lines)
        {
            if (line.Length > 0 && line.StartsWith(address))
            {
                var items = line.Split(';');
                if (items.Length > 1) return items[1];
                else return null;
            }
        }
        return null;
    }
}
