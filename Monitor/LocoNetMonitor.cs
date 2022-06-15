using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor;
internal sealed class LocoNetMonitor : BackgroundService, IDisposable
{ 
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<LocoNetMonitor> _logger;
    private readonly LocoNetInterface _locoNetInterface;
    private readonly IPEndPoint _listenEndPoint;
    private readonly UdpClient _listener;

    public LocoNetMonitor(IOptions<AppSettings> options, LocoNetInterface locoNetInterface, ILogger<LocoNetMonitor> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetInterface = locoNetInterface;
        _listenEndPoint = new IPEndPoint(IPAddress.Any, Settings.Udp.SendPort); // SendPort is port used by services sending UDP to this service.
        _listener = new UdpClient(_listenEndPoint);
    }
    AppSettings Settings => _options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _listener.ReceiveAsync(stoppingToken);
                if (result.Buffer.Length > 0)
                {
                    await _locoNetInterface.Write(result.Buffer, stoppingToken);
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
                _locoNetInterface.Dispose();
                _listener.Dispose();
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
