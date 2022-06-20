using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor;
internal class LocoNetBroadcaster : BackgroundService, IDisposable
{
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<LocoNetBroadcaster> _logger;
    private readonly LocoNetInterface _locoNetInterface;
    private readonly IPEndPoint _broadcastEndPoint;
    private readonly UdpClient _broadcaster;


    public LocoNetBroadcaster(IOptions<AppSettings> options, LocoNetInterface locoNetInterface, ILogger<LocoNetBroadcaster> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetInterface = locoNetInterface;
        _broadcastEndPoint = new(IPAddress.Parse(Settings.Udp.BroadcastIPAddress), Settings.Udp.BroadcastPort);
        _broadcaster = new()
        {
            EnableBroadcast = true,
            ExclusiveAddressUse = false,
        };
    }
    AppSettings Settings => _options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        byte[] overflow = Array.Empty<byte>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var data = await _locoNetInterface.WaitForData(stoppingToken);
            if (data is not null && data.Length > 0)
            {
                var packets = overflow.Concat(data).ToArray().Split();
                overflow = Array.Empty<byte>();
                foreach (var packet in packets)
                {
                    if (packet.IsComplete)
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

