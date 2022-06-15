using Microsoft.Extensions.Options;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor;

public class Broadcaster : BackgroundService
{
    private readonly ILogger<Broadcaster> _logger;
    private readonly IOptions<MonitorSettings> _options;
    private readonly IPEndPoint _broadcastEndPoint;
    private readonly IPEndPoint _listeningEndPoint;
    private readonly UdpClient _broadcastUdpClient;
    private readonly UdpClient _listeningUdpClient;
    private readonly SerialPort _locoNetPort;
    MonitorSettings Settings => _options.Value;

    public Broadcaster(IOptions<MonitorSettings> options, ILogger<Broadcaster> logger)
    {
        _options = options;
        _logger = logger;
        _listeningEndPoint = new IPEndPoint(IPAddress.Any, Settings.Udp.SendPort);
        _listeningUdpClient = new UdpClient(_listeningEndPoint);
        _broadcastEndPoint = new(IPAddress.Parse(Settings.Udp.BroadcastIPAddress), Settings.Udp.BroadcastPort);
        _broadcastUdpClient = new()
        {
            EnableBroadcast = true,
            ExclusiveAddressUse = false,
        };
        _locoNetPort = new(Settings.LocoNet.Port, Settings.LocoNet.BaudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = Settings.LocoNet.ReadTimeout
        };
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => ForwardLocoNetMessages(cancellationToken), cancellationToken);
        return base.StartAsync(cancellationToken);
    }

    private async Task ForwardLocoNetMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _listeningUdpClient.ReceiveAsync(cancellationToken);
                if (result.Buffer.Length > 0)
                {
                    _locoNetPort.Write(result.Buffer, 0, result.Buffer.Length);
                    _logger.LogDebug("To LocoNet: {message}", result.Buffer.ToHex());
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{message}", ex.Message);
            }
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LocoNet broadcaster start reading from {port} at {time}", Settings.LocoNet.Port, DateTimeOffset.Now);
        _locoNetPort.Open();
        bool init = true;
        byte[] overflow = Array.Empty<byte>();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_locoNetPort.BytesToRead > 0)
            {
                try
                {
                    var size = _locoNetPort.BytesToRead;
                    var data = new byte[size + overflow.Length];
                    overflow.CopyTo(data, 0);
                    overflow = Array.Empty<byte>();
                    var count = _locoNetPort.Read(data, overflow.Length, size);
                    if (init) { init = false; continue; }
                    if (count > 0)
                    {
                        var packets = data.Split();
                        foreach (var packet in packets)
                        {
                            if (packet.IsComplete)
                            {
                                _logger.LogDebug("Broadcast packet {packet}", packet);
                                var sent = await _broadcastUdpClient.SendAsync(packet.Data, packet.Length, _broadcastEndPoint);
                                if (sent == 0) _logger.LogWarning("Packet not sent.");
                            }
                            else
                            {
                                overflow = packet.Data;
                            }
                        }

                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("LocoNet read timeout.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("{message}", ex.Message);
                    _broadcastUdpClient.Close();
                    _locoNetPort.Close();
                }
            }
            else
            {
                await Task.Delay(10, stoppingToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listeningUdpClient.Dispose();
        _broadcastUdpClient.Dispose();
        _locoNetPort.Dispose();
        return Task.CompletedTask;
    }
}
