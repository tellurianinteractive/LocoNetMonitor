using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tellurian.Trains.LocoNetMonitor.Services;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Services;

[TestClass]
public  class CsvFileLocoOwnerServiceTests
{
    [TestMethod]
    public void AllAddressesAreUnique()
    {
        var options = Options.Create(new AppSettings() { CsvFileLocoAddressOwnerService = new CsvFileLocoAddressOwnerServiceSettings() { LocoOwnersListCsvFilePath = @"C:\Temp\Loklista.txt" } });
        var target = new CsvFileLocoOwnerService(options, new NullLogger<CsvFileLocoOwnerService>());
        var x = target.GetOwner(82);
        Assert.IsNotNull(x);
    }
}