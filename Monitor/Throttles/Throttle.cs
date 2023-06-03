using System.Net;
using System.Net.Sockets;
using System.Text;
using Tellurian.Trains.LocoNetMonitor.Slots;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;

internal class Throttle : IDisposable
{
    public const int MaxLocos = 4;
    private readonly ILogger _logger;
    private readonly TcpClient _connection;
    private readonly NetworkStream _stream;
    private readonly Task _receiveTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly WiThrottleServerSettings _settings;
    private readonly SlotTable _slotTable;
    private readonly List<Loco> _locos = new();
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;

    public Throttle(TcpClient connection, WiThrottleServerSettings settings, SlotTable slotTable, ILogger logger)
    {
        _connection = connection;
        _settings = settings;
        _slotTable = slotTable;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
        _stream = connection.GetStream();
        _stream.ReadTimeout = _settings.Timeout * 1000;
        _receiveTask = ReceiveAsync(_cancellationTokenSource.Token);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.Timeout));
        _timerTask = OnTimerAsync(_cancellationTokenSource.Token);
    }

    public bool IsConnected { get; private set; }
    public bool IsDisconnected => !IsConnected;
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public async Task Initialize()
    {
        IsConnected = true;
        await SendMessageAsync(WiThrottleServer.MinVersion);
        await SendMessageAsync($"HTTellurian");
        await SendMessageAsync($"HtTellurian wiThrottle server {ProgramInfo.Version}");
        await SendMessageAsync($"*{_settings.Timeout}");
        await Task.Run(() => _receiveTask.ConfigureAwait(false));
        await Task.Run(() => _timerTask.ConfigureAwait(false));
    }

    private async Task OnTimerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                await SendMessageAsync("*");
           }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Heartbeat timer is stopped.");
            }
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        try
        {
            await _stream.WriteAsync(message.AsBytes()).ConfigureAwait(false);
            await _stream.WriteAsync(Environment.NewLine.AsBytes()).ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);

        }
        catch (ObjectDisposedException)
        {

            _logger.LogWarning("Cannot send message {message} because TCP connection is closed.", message);
        }
    }

    private async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        const int bufferSize = 1024;
        var p = 0;
        var buffer = new byte[bufferSize];
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var x = _stream.ReadByte();
                if (x == -1) break;
                var b = (byte)x;
                if (p > 0 && b == 0xA0 || b == 0x0D)
                {
                    await ProcessMessage(Encoding.UTF8.GetString(buffer, 0, p).Trim('\n', '\r', ' '));
                    p = 0;
                }
                else
                {
                    buffer[p++] = b;
                }
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("Cannot receive message because TCP connection is closed.");
                break;
            }
            catch (IOException ex)
            {
                _logger.LogWarning("Throttle didn't respond within {timeout} seconds. {message}", _settings.Timeout, ex.Message);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network stream cannot be read.");
                break;
            }
        }
        await SetSpeed(SpeedCommand.Create(null, 1));
        await SetSpeed(SpeedCommand.Create(null, 0));
        _logger.LogWarning("Throttle {ipaddress} {name} disconnected and its controlled locos are stopped.", EndPoint!.Address, Name);
        if (_connection.Connected) _connection.Close();
        _cancellationTokenSource.Cancel();
        IsConnected = false;
    }

    private async Task ProcessMessage(string message)
    {
        _logger.LogDebug("From {ipaddress}: {text}", EndPoint?.Address, message);
        if (Command.TryParse(message, out var entry))
        {
            if (entry is Heartbeat) HandleHeartbeat();
            else if (entry is SpeedCommand speedEntry) await SetSpeed(speedEntry);
            else if (entry is FunctionCommand functionCommand) await SetFunction(functionCommand);
            else if (entry is AssignCommand assignEntry) await AddLocoToThrottle(assignEntry);
            else if (entry is DispatchCommand dispatchEntry) await DispatchLocoThrottle(dispatchEntry);
            else if (entry is ReleaseCommand releaseEntry) await RemoveLocoThrottle(releaseEntry);
            else if (entry is NameCommand nameEntry) Name = nameEntry.Name;

            if (entry.HasReply) await SendMessageAsync(entry.Reply);
        }
        else
        {
            _logger.LogWarning("Unsupported message from {ipaddress}: {text}", EndPoint?.Address, message);
        }
    }

    private void HandleHeartbeat()
    {
        _logger.LogDebug("Heartbeat from throttle IP {ipaddress} {name}.", EndPoint?.Address, Name);
    }

    private async Task AddLocoToThrottle(AssignCommand entry)
    {
        if (_locos.Count > MaxLocos) _logger.LogWarning("Throttle {ipaddress} cannot assign {key}. Max {max} locos is exceeded.", EndPoint?.Address, entry.Key, MaxLocos);
        var loco = _locos.SingleOrDefault(l => l.Key == entry.Key);
        if (loco is null) _locos.Add(new(entry.ThrottleId, entry.Key!, entry.Address));
        await _slotTable.RequestAddress(entry.Address, _cancellationTokenSource.Token);
    }

    private async Task RemoveLocoThrottle(ReleaseCommand entry)
    {
        var loco = _locos.SingleOrDefault(l => l.Key == entry.Key);
        if (loco is not null)
        {
            _locos.Remove(loco);
            var slots = _slotTable.FindByLocoAddress(loco.Address);
            foreach (var slot in slots) await _slotTable.SetSlotInactive(slot, _cancellationTokenSource.Token);
        }
    }

    private async Task DispatchLocoThrottle(DispatchCommand entry)
    {
        var loco = _locos.SingleOrDefault(l => l.Key == entry.Key);
        if (loco is not null)
        {
            _locos.Remove(loco);
            var slots = _slotTable.FindByLocoAddress(loco.Address);
            foreach (var slot in slots) await _slotTable.DispatchSlot(slot, _cancellationTokenSource.Token);
        }
    }

    private async Task SetSpeed(SpeedCommand entry)
    {
        var locos = entry.All ? _locos : _locos.Where(l => l.Key == entry.Key);
        foreach (var loco in locos)
        {
            var slots = _slotTable.FindByLocoAddress(loco.Address);
            if (slots is null) continue;
            foreach (var slot in slots)
            {
                await _slotTable.SetSlotSpeed(slot, entry.Speed, _cancellationTokenSource.Token);
            }
        }
    }

    private async Task SetFunction(FunctionCommand command)
    {
        var locos = command.All ? _locos : _locos.Where(l => l.Key == command.Key);
        foreach (var loco in locos)
        {
            var slots = _slotTable.FindByLocoAddress(loco.Address);
            if (slots is null) continue;
            foreach (var slot in slots)
            {
                await _slotTable.SetSlotFunction(slot, command.Function, command.IsOn, _cancellationTokenSource.Token);
            }

        }
    }

    public IPEndPoint? EndPoint => (IPEndPoint?)(_connection.Client?.RemoteEndPoint);

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _receiveTask.Wait();
                _timerTask.Dispose();
                _connection.Close();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
