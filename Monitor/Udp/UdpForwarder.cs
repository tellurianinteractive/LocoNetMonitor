using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Udp;
internal sealed class UdpForwarder : BackgroundService, IDisposable
{
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<UdpForwarder> _logger;
    private readonly ISerialPortGateway _locoNetGateway;

    public UdpForwarder(IOptions<AppSettings> options, ISerialPortGateway locoNetGateway, ILogger<UdpForwarder> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetGateway = locoNetGateway;
    }
    AppSettings Settings => _options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var localEndPoint = new IPEndPoint(IPAddress.Any, Settings.UdpForwarder.LocalPortNumber);
        var listener = new UdpClient();
        listener.Client.Bind(localEndPoint);
        listener.JoinMulticastGroup(Settings.MulticastIPAddress());
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await listener.ReceiveAsync(stoppingToken);
                if (result.Buffer.IsValidMessage())
                {
                    await _locoNetGateway.Write(result.Buffer, stoppingToken);
#if DEBUG
                    _logger.LogDebug("To LocoNet: {message}", result.Buffer.ToHex());
#endif
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{message}", ex.Message);
            }

        }
    }

    #region Dispose

    private bool disposedValue;
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (_locoNetGateway is IDisposable disposable) disposable.Dispose();
            }
            disposedValue = true;
        }
    }

    public override void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
