namespace Tellurian.Trains.LocoNetMonitor;
public class LocoNetMessageQueue
{
    readonly List<QueuedMessage> _queue = new(100);
    public void AddOrUpdate(byte[] locoNetMessage)
    {
        if (locoNetMessage is null || locoNetMessage.Length < 2) return;
        var queuedMessage = new QueuedMessage(locoNetMessage);
        if (queuedMessage.OperationsCode == 0xA0)
        {
            lock (_queue)
            {
                var index = _queue.IndexOf(queuedMessage);
                if (index >= 0) _queue[index] = queuedMessage;
                else _queue.Add(queuedMessage);
            }
        }
        else
        {
            _queue.Add(queuedMessage);
        }
    }

    public byte[] TryGetNextMessage()
    {
        lock (_queue)
        {
            if (_queue.Count == 0) return Array.Empty<byte>();
            var item = _queue.OrderBy(item => item.Priority).First();
            _queue.Remove(item);
            return item.Data;
        } 
    }
}

public sealed class QueuedMessage
{
    public QueuedMessage(byte[] data) => Data = data;
    public byte[] Data { get; }
    public byte OperationsCode => Data[0];
    public byte SlotNumber => Data.SlotNumber();

    private readonly DateTimeOffset Timestamp = DateTimeOffset.Now;
    private TimeSpan Delay => DateTimeOffset.Now - Timestamp;
    public int Priority => Data.Priority() - Delay.Milliseconds;
    public override bool Equals(object? obj) => obj is QueuedMessage qm && qm.OperationsCode == OperationsCode && qm.SlotNumber == SlotNumber;
    public override int GetHashCode() => HashCode.Combine(OperationsCode, SlotNumber);
}
