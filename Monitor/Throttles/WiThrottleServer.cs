using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Tellurian.Trains.LocoNetMonitor.Slots;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;
internal class WiThrottleServer : BackgroundService
{
    public const string MinVersion = "VN2.0";

    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<WiThrottleServer> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<IPAddress, Throttle> _throttles;
    private readonly ISlotTable _slotTable;
    public WiThrottleServer(IOptions<AppSettings> options, ISlotTable slotTable, ITimeProvider timeProvider, ILogger<WiThrottleServer> logger)
    {
        _options = options;
        _slotTable = slotTable;
        _timeProvider = timeProvider;
        _logger = logger;
        _throttles = new ConcurrentDictionary<IPAddress, Throttle>(Environment.ProcessorCount, Settings.Backlog);
    }

    private WiThrottleServerSettings Settings => _options.Value.WiThrottleServer;

    public Task DebugExecuteAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    public IEnumerable<Throttle> Throttles => _throttles.Values;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WiThrottle server is starting...");
        var tcpListener = new TcpListener(IPAddress.Any, Settings.PortNumber) { ExclusiveAddressUse = false };
        tcpListener.Start(Settings.Backlog);
        _logger.LogInformation("TCP listener started listening on endpoint {localEndPoint}", tcpListener.LocalEndpoint);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var connection = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                _logger.LogDebug("TCP accepted connection from {RemoteEndPoint}", connection.Client.RemoteEndPoint);
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
                    _throttles.TryAdd(iPAddress, throttle);
                }
                await throttle.Initialize();
                _logger.LogInformation("Throttle {Throttle} created", throttle);
            }
        }

        void RemoveClosedThrottles()
        {
            foreach (var throttle in _throttles.Where(t => t.Value.IsDisconnected))
            {
                throttle.Value.Dispose();
                _throttles.TryRemove(throttle);
            }
        }
    }


    private bool TryCreateThrottle(TcpClient connection, [NotNullWhen(true)] out Throttle? throttle, [NotNullWhen(true)] out IPAddress? iPAddress)
    {
        if (connection.Client.RemoteEndPoint is IPEndPoint endPoint && connection.Connected)
        {
            throttle = new Throttle(connection, Settings, _slotTable, _timeProvider, _logger);
            iPAddress = endPoint.Address;
            return true;
        }
        throttle = null;
        iPAddress = null;
        return false;
    }
}
