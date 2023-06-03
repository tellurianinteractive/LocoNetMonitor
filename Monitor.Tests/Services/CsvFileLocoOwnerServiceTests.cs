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
        var options = Options.Create(new AppSettings() { CsvFileLocoAddressOwnerService = new CsvFileLocoAddressOwnerServiceSettings() { LocoOwnersListCsvFilePath = @"Test data\Loklista.txt" } });
        var target = new CsvFileLocoOwnerService(options, new NullLogger<CsvFileLocoOwnerService>());
        var owner = target.GetOwner(56);
        Assert.IsNotNull(owner);
    }
}