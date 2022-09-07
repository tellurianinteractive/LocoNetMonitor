using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tellurian.Trains.LocoNetMonitor.Tests.LocoNet;

[TestClass]
public class ExplorationTests
{
    [TestMethod]
    public void DccAddressComponents()
    {
        var cv17 = (4711 / 256) + 192;
        var cv18 = 4711 & 0xFF;
        Assert.AreEqual(210, cv17);
        Assert.AreEqual(103, cv18);
    }
}
