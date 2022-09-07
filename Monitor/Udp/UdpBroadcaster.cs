using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Udp;
internal class UdpBroadcaster : BackgroundService, IDisposable
{
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<UdpBroadcaster> _logger;
    private readonly ISerialPortGateway _locoNetGateway;
    private readonly IPEndPoint _broadcastEndPoint;
    private readonly UdpClient _broadcaster;


    public UdpBroadcaster(IOptions<AppSettings> options, ISerialPortGateway locoNetGateway, ILogger<UdpBroadcaster> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetGateway = locoNetGateway;
        _broadcastEndPoint = new(Settings.MulticastIPAddress(), Settings.UdpBroadcaster.LocalPortNumber);
        _broadcaster = new()
        {
            EnableBroadcast = true,
        };
    }
    AppSettings Settings => _options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        byte[] overflow = Array.Empty<byte>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var data = await _locoNetGateway.WaitForData(stoppingToken);
                if (data is not null && data.Length > 0)
                {
                    var packets = overflow.Concat(data).ToArray().AsPackets();
                    overflow = Array.Empty<byte>();
                    foreach (var packet in packets)
                    {
                        if (packet.IsComplete && packet.Data.IsValidMessage())
                        {
                            _logger.LogDebug("Broadcast packet {packet}", packet);
                            var sent = await _broadcaster.SendAsync(packet.Data, packet.Length, _broadcastEndPoint);
                            if (sent == 0) _logger.LogDebug("Packet not sent.");
                        }
                        else
                        {
                            overflow = packet.Data;
                        }
                    }
                }

            }
            catch (FileNotFoundException)
            {
                _logger.LogError("Serial port is not found");
                break;
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
                (_locoNetGateway as IDisposable)?.Dispose();
                _broadcaster.Dispose();
            }
            disposedValue = true;
        }
    }

    public override void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
        base.Dispose();
    }
    #endregion
}

