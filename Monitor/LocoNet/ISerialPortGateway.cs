namespace Tellurian.Trains.LocoNetMonitor.LocoNet;

internal interface ISerialPortGateway: IDisposable, IAsyncDisposable
{
    Task<byte[]> WaitForData(CancellationToken cancellationToken);
    ValueTask Write(byte[] data, CancellationToken stoppingToken);
}