using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Udp;

public sealed class UdpGateway : IDisposable 
{ 
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<UdpGateway> _logger;
    private readonly UdpClient _client;
    private readonly IPEndPoint _localEndPoint;
    private readonly IPEndPoint _forwarderEndPoint;
    public UdpGateway(IOptions<AppSettings> options, int localPortNumber, ILogger<UdpGateway> logger)
    {
        _options = options;
        _logger = logger;

        _forwarderEndPoint = new(Settings.MulticastIPAddress(), Settings.UdpForwarder.LocalPortNumber);
        
        _localEndPoint = new IPEndPoint(IPAddress.Any, localPortNumber);
        _client = new UdpClient() { ExclusiveAddressUse = false };
        _client.JoinMulticastGroup(Settings.MulticastIPAddress());
        _client.Client.Bind(_localEndPoint);
    }

    private AppSettings Settings => _options.Value;
    
    public async Task<byte[]> WaitForData(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.ReceiveAsync(cancellationToken);
            return result.Buffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed recieving LocoNet message from UDP");
        }
        return Array.Empty<byte>();
    }

    public async ValueTask Write(byte[] data, CancellationToken stoppingToken)
    {
        if (data is null || data.Length == 0) return;
        var memory = new ReadOnlyMemory<byte>(data);
        try
        {
            await _client.SendAsync(memory, _forwarderEndPoint, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot send LocoNet message to UDP");
        }
    } 
    
    public void Dispose() => _client.Dispose();
}