using System.Net;
using System.Net.Sockets;
using System.Text;
using Tellurian.Trains.LocoNetMonitor.Slots;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;

internal class Throttle : IDisposable
{
    public const int MaxLocos = 4;
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TcpClient _connection;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly WiThrottleServerSettings _settings;
    private readonly ISlotTable _slotTable;
    private readonly List<Loco> _locos = new();
    private readonly PeriodicTimer _timer;

    private Task _receiveTask = Task.CompletedTask;
    private Task _timerTask = Task.CompletedTask;

    public Throttle(TcpClient connection, WiThrottleServerSettings settings, ISlotTable slotTable, ITimeProvider timeProvider, ILogger logger)
    {
        _connection = connection;
        _settings = settings;
        _timeProvider = timeProvider;
        _slotTable = slotTable;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
        _stream = connection.GetStream();
        _stream.ReadTimeout = _settings.Timeout * 1000;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.Timeout));
        _timerTask = new Task(async () => await OnTimerAsync(_cancellationTokenSource.Token));
    }

    public bool IsConnected { get; private set; }
    public bool IsDisconnected => !IsConnected;
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset? LastHeartbeat { get; private set; }

    public async Task Initialize()
    {
        IsConnected = true;
        await SendMessageAsync(WiThrottleServer.MinVersion);
        await SendMessageAsync($"HTTellurian");
        await SendMessageAsync($"HtTellurian wiThrottle server {ProgramInfo.Version}");
        await SendMessageAsync($"*{_settings.Timeout}");
        _receiveTask = Task.Run(async () => await ReceiveAsync(_cancellationTokenSource.Token));
        _timerTask = Task.Run(async () => await OnTimerAsync(_cancellationTokenSource.Token));
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
                _logger.LogWarning("Heartbeat timer is canceled.");
            }
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        try
        {
            await _stream.WriteAsync(message.AsBytes('\n')).ConfigureAwait(false);
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
                var bytesRead = await _stream.ReadAsync(buffer, stoppingToken);
                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var messages = data.Split('\n').Where(m => m.Length > 0);
                foreach (var message in messages) await ProcessMessage(message);

            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("Cannot receive message because TCP connection is closed.");
                break;
            }
            catch (IOException ex)
            {
                _logger.LogWarning("Throttle didn't respond within {Timeout} seconds. {Message}", _settings.Timeout, ex.Message);
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
        _logger.LogWarning("Throttle {EndPoint} {ThrottleName} disconnected and its controlled locos are stopped.", EndPoint, Name);
        if (_connection.Connected) _connection.Close();
        _cancellationTokenSource.Cancel();
        IsConnected = false;
    }

    private async Task ProcessMessage(string message)
    {
        if (Command.TryParse(message, out var entry))
        {
            if (entry is Heartbeat) HandleHeartbeat();
            else if (entry is SpeedCommand speedEntry) await SetSpeed(speedEntry);
            else if (entry is FunctionCommand functionCommand) await SetFunction(functionCommand);
            else if (entry is AssignCommand assignEntry) await AddLocoToThrottle(assignEntry);
            else if (entry is DispatchCommand dispatchEntry) await DispatchLocoThrottle(dispatchEntry);
            else if (entry is ReleaseCommand releaseEntry) await RemoveLocoThrottle(releaseEntry);
            else if (entry is NameCommand nameEntry) Name = nameEntry.Name;
            _logger.LogDebug("From {EndPoint}: {Message} {CommandType}", EndPoint, message, entry.GetType().Name);

            if (entry.HasReply) await SendMessageAsync(entry.Reply);
        }
        else
        {
            _logger.LogWarning("Unsupported message from {EndPoint}: {Message}", EndPoint, message);
        }
    }

    private void HandleHeartbeat()
    {
        LastHeartbeat = _timeProvider.UtcNow;
    }

    private async Task AddLocoToThrottle(AssignCommand entry)
    {
        if (_locos.Count >= MaxLocos) _logger.LogWarning("Throttle {EndPoint} cannot assign {ThrottleKey}. Max {MaxLocos} locos is exceeded.", EndPoint, entry.Key, MaxLocos);
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

    public override string ToString() => $"{EndPoint} {string.Join(",", _locos.Select(l => l.Address))}";

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _stream?.Dispose();
                _connection.Close();
                _timer?.Dispose();
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
