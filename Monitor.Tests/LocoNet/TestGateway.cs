namespace Tellurian.Trains.LocoNetMonitor.Tests.LocoNet;
internal class TestGateway: ISerialPortGateway
{
    private readonly List<byte[]> _writtenMessages = new();
    public readonly List<byte[]> MessagesToSend = new();
    public ICollection<byte[]> WrittenMessages => _writtenMessages;


    public Task<byte[]> WaitForData(CancellationToken cancellationToken)
    {
        var message = MessagesToSend.FirstOrDefault();
        if (message is null) return Task.FromResult(Array.Empty<byte>());
        MessagesToSend.Remove(message);
        return Task.FromResult(message);
    }
    
    public ValueTask Write(byte[] message, CancellationToken stoppingToken)
    {
        _writtenMessages.Add(message);
        return ValueTask.CompletedTask;
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
