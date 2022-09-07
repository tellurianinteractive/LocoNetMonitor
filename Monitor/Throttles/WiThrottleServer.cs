using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tellurian.Trains.LocoNetMonitor.Slots;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;
internal class WiThrottleServer : BackgroundService
{
    public const string MinVersion = "VN2.0";

    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<WiThrottleServer> _logger;
    private readonly IDictionary<IPAddress, Throttle> _throttles;
    private readonly SlotTable _slotTable;
    public WiThrottleServer(IOptions<AppSettings> options, SlotTable slotTable, ILogger<WiThrottleServer> logger)
    {
        _options = options;
        _slotTable = slotTable;
        _logger = logger;
        _throttles = new Dictionary<IPAddress, Throttle>(Settings.Backlog);
    }

    private WiThrottleServerSettings Settings => _options.Value.WiThrottleServer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WiThrottle server is starting...");
        var tcpListener = new TcpListener(IPAddress.Any, Settings.PortNumber) { ExclusiveAddressUse = false };
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                tcpListener.Start(Settings.Backlog);
                _logger.LogInformation("TCP listener started listening on endpoint {endport}", tcpListener.LocalEndpoint);

                var connection = await tcpListener.AcceptTcpClientAsync(stoppingToken);
                RemoveClosedThrottles();
                await TryCreateThrottle(connection);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "TCP listener cannot be started. Error code {code}", ex.ErrorCode);
            }
            finally
            {
            }
        }

        try
        {
            tcpListener.Stop();
            _logger.LogInformation("TCP listener stopped.");
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "TCP listener cannot be stopped. Error code {code}", ex.ErrorCode);
        }

        async Task TryCreateThrottle(TcpClient connection)
        {
            if (this.TryCreateThrottle(connection, out var throttle, out var iPAddress))
            {
                if (_throttles.ContainsKey(iPAddress))
                {
                    _throttles[iPAddress].Dispose();
                    _throttles[iPAddress] = throttle;
                }
                else
                {
                    _throttles.Add(iPAddress, throttle);
                }
                await throttle.Initialize();

            }
        }

        void RemoveClosedThrottles()
        {
            foreach (var t in _throttles.Values.Where(t => t.IsDisconnected))
            {
                t.Dispose();
                _throttles.Remove(t.EndPoint!.Address);
            }
        }
    }


    private bool TryCreateThrottle(TcpClient connection, [NotNullWhen(true)] out Throttle? throttle, [NotNullWhen(true)] out IPAddress? iPAddress)
    {
        if (connection.Client.RemoteEndPoint is IPEndPoint endPoint && connection.Connected)
        {
            throttle = new Throttle(connection, Settings, _slotTable, _logger);
            iPAddress = endPoint.Address;
            return true;
        }
        throttle = null;
        iPAddress = null;
        return false;
    }
}

public record Loco(char Id, string Key, int Address);

public static class EncodingExtensions
{
    public static byte[] AsBytes(this string? text) =>
        string.IsNullOrWhiteSpace(text) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(text);

}
