using Tellurian.Trains.LocoNetMonitor.Throttles;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

[TestClass]
public class WiThrottleParserTests
{
    [TestMethod]
    public void NotSupportedEntry_ReturnsNull()
    {
        var actual = "XXX".AsEntry();
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReturnsNameEntry()
    {
        var actual = "NDSB ME 1515".AsEntry() as NameCommand;
        Assert.IsInstanceOfType(actual, typeof(NameCommand));
        Assert.AreEqual("DSB ME 1515", actual!.Name);
    }

    [TestMethod]
    public void ReturnsIdEntry()
    {
        var actual = "HU11ebefc7f399f161".AsEntry() as IdCommand;
        Assert.IsInstanceOfType(actual, typeof(IdCommand));
        Assert.AreEqual("11ebefc7f399f161", actual!.Id);
    }

    [TestMethod]
    public void ReturnsHeartbeatEntry()
    {
        var actual = "*10".AsEntry() as Heartbeat;
        Assert.IsInstanceOfType(actual, typeof(Heartbeat));
        Assert.AreEqual(10, actual!.Timeout);
    }

    [TestMethod]
    public void ReturnsDispatchEntryAll()
    {
        var actual = "MT-*<;>d".AsEntry() as DispatchCommand;
        Assert.IsInstanceOfType(actual, typeof(DispatchCommand));
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);
        Assert.AreEqual("MT-*<;>", actual!.Reply);
    }

    [TestMethod]
    public void ReturnsDispatchEntryWithAddress()
    {
        var actual = "MT-L341<;>d".AsEntry() as DispatchCommand;
        Assert.IsInstanceOfType(actual, typeof(DispatchCommand));
        Assert.AreEqual("L341", actual!.Key);
        Assert.IsFalse(actual!.All);
        Assert.AreEqual("MT-L341<;>", actual!.Reply);
    }

    [TestMethod]
    public void ReturnsReleaseEntryAll()
    {
        var actual = "MT-*<;>r".AsEntry() as ReleaseCommand;
        Assert.IsInstanceOfType(actual, typeof(ReleaseCommand));
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);
        Assert.AreEqual("MT-*<;>", actual!.Reply);
    }

    [TestMethod]
    public void ReturnsReleaseEntryWithAddress()
    {
        var actual = "MT-L341<;>r".AsEntry() as ReleaseCommand;
        Assert.IsInstanceOfType(actual, typeof(ReleaseCommand));
        Assert.AreEqual("L341", actual!.Key);
        Assert.IsFalse(actual!.All);
        Assert.AreEqual("MT-L341<;>", actual!.Reply);
    }

    [TestMethod]
    public void ReturnSpeedEntryForAll()
    {
        var actual = "MTA*<;>V127".AsEntry() as SpeedCommand;
        Assert.IsInstanceOfType(actual, typeof(SpeedCommand));
        Assert.AreEqual(127, actual!.Speed);
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);

    }

    [TestMethod]
    public void ReturnSpeedEntryForAddress()
    {
        var actual = "MTAS37<;>V127".AsEntry() as SpeedCommand;
        Assert.IsInstanceOfType(actual, typeof(SpeedCommand));
        Assert.AreEqual(127, actual!.Speed);
        Assert.AreEqual("S37", actual!.Key);
        Assert.IsFalse(actual!.All);

    }

    [TestMethod]
    public void ReturnFunctionEntryForAll()
    {
        var actual = "MTA*<;>F012".AsEntry() as FunctionCommand;
        Assert.IsInstanceOfType(actual, typeof(FunctionCommand));
        Assert.AreEqual(12, actual!.Function);
        Assert.IsFalse(actual!.IsOn);
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);
        Assert.IsTrue(actual!.IsPush);
    }

    [TestMethod]
    public void ReturnFunctionEntryForAddress()
    {
        var actual = "MTAL1234<;>f113".AsEntry() as FunctionCommand;
        Assert.IsInstanceOfType(actual, typeof(FunctionCommand));
        Assert.AreEqual(13, actual!.Function);
        Assert.IsTrue(actual!.IsOn);
        Assert.AreEqual("L1234", actual!.Key);
        Assert.IsFalse(actual!.All);
        Assert.IsFalse(actual!.IsPush);
    }

    [TestMethod]
    public void ReturnDirectionEntryForAll()
    {
        var actual = "MTA*<;>R0".AsEntry() as DirectionCommand;
        Assert.IsInstanceOfType(actual, typeof(DirectionCommand));
        Assert.IsFalse(actual!.IsForward);
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);
    }

    [TestMethod]
    public void ReturnDirectionEntryForAddress()
    {
        var actual = "MTAL9999<;>R1".AsEntry() as DirectionCommand;
        Assert.IsInstanceOfType(actual, typeof(DirectionCommand));
        Assert.IsTrue(actual!.IsForward);
        Assert.AreEqual("L9999", actual!.Key);
        Assert.IsFalse(actual!.All);
    }

    [TestMethod]
    public void ReturnIdleEntryForAll()
    {
        var actual = "MTA*<;>I".AsEntry() as Idle;
        Assert.IsInstanceOfType(actual, typeof(Idle));
        Assert.IsNull(actual!.Key);
        Assert.IsTrue(actual!.All);
    }

    [TestMethod]
    public void ReturnIdleEntryForAddress()
    {
        var actual = "MTAL9998<;>I".AsEntry() as Idle;
        Assert.IsInstanceOfType(actual, typeof(Idle));
        Assert.AreEqual("L9998", actual!.Key);
        Assert.IsFalse(actual!.All);
    }

    [TestMethod]
    public void ReturnAssignEntryForAddress()
    {
        var actual = "MT+L112<;>L112".AsEntry() as AssignCommand;
        Assert.IsInstanceOfType(actual, typeof(AssignCommand));
        Assert.AreEqual('T', actual!.ThrottleId);
        Assert.AreEqual("L112", actual!.Key);
        Assert.AreEqual(112, actual!.Address);
        Assert.AreEqual("MT+L112<;>", actual!.Reply);
    }
    [TestMethod]
    public void ReturnAssignEntryForLongAddress()
    {
        var actual = "M0+L4711<;>L4711".AsEntry() as AssignCommand;
        Assert.IsInstanceOfType(actual, typeof(AssignCommand));
        Assert.AreEqual('0', actual!.ThrottleId);
        Assert.AreEqual("L4711", actual!.Key);
        Assert.AreEqual(4711, actual!.Address);
        Assert.AreEqual("M0+L4711<;>", actual!.Reply);
    }
}
