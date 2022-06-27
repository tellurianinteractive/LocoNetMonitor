namespace Tellurian.Trains.LocoNetMonitor.Tests;


[TestClass]
public class LocoNetPriorityTests
{
    [TestMethod]
    public void ComparesPriorityCorrectly()
    {
        var q1 = new byte[] { 0xA0, 1, 0, 0 };
        var q2 = new byte[] { 0xA1, 1, 0, 0 };
        var target = new LocoNetMessageQueue();
        target.AddOrUpdate(q2);
        target.AddOrUpdate(q1);
        var r1 = target.TryGetNextMessage();
        Assert.AreEqual(q1, r1);
    }
}
