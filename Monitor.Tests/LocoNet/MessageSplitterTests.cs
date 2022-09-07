using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tellurian.Trains.LocoNetMonitor.Tests.LocoNet;

[TestClass]
public class MessageSplitterTests
{
    [TestMethod]
    public void FindStartOfMessage()
    {
        var data = new byte[] { 0x20, 0x20, 0x81, 0x00 };
        Assert.AreEqual(2, data.IndexOfFirstOperationCode());
    }

    [TestMethod]
    public void SplitsMessagesIntoPackets()
    {
        var data = new byte[] { 0xA0, 0x01, 0x00, 0x5E, 0xA0, 0x01, 0x00, 0x5E };
        Assert.AreEqual(2, data.AsPackets().Count());

    }
}
