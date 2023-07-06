using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tellurian.Trains.LocoNetMonitor.Extensions;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;

/// <summary>
/// Simulates a wiFred in a test freindly way
/// Implemended inspired by https://github.com/newHeiko/wiFred/tree/master/software/esp-firmware
/// </summary>
internal sealed class WiFredSimulator : IDisposable
{
    public WiFredSimulator(WiFredSimulatorSettings settings, ITimeProvider timeProvider, ILogger logger)
    {
        Locos = new();
        TcpConnection = new TcpClient();
        CancellationTokenSource = new CancellationTokenSource();
        State = ThrottleState.StartingUp;
        Settings = settings;
        TimeProvider = timeProvider;
        Logger = logger;
    }
    /// <summary>
    /// To simulate pressing the red emergensy button.
    /// </summary>
    public bool IsEmergencyStop { get; set; }
    /// <summary>
    /// To simulate that the wiFRED has empty battery.
    /// </summary>
    public bool HasEmptyBattery { get; set; }
    /// <summary>
    /// To control the time interval for connection keep alive.
    /// </summary>
    public TimeSpan KeepAliveTimeout => Settings.KeepAliveTimeout;
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan NoActivityTimeout => Settings.NoActivityTimeout;

    private IPEndPoint ServerEndPoint => Settings.ServerEndPoint;

    private readonly WiFredSimulatorSettings Settings;
    private readonly TcpClient TcpConnection;
    private readonly Locos Locos;
    private readonly ITimeProvider TimeProvider;
    private readonly ILogger Logger;
    private readonly CancellationTokenSource CancellationTokenSource;

    private Task? ReceiveTask;
    private Task? RunTask;
    private NetworkStream Stream;

    private ThrottleState State;
    private DateTimeOffset LastHeartBeat;
    private DateTimeOffset LastSpeedUpdate;
    private DateTimeOffset LastActivity;

    private byte SpeedOfAttachedLocos { get; set; }
    private bool IsReversing;
    private bool disposedValue;

    public IEnumerable<string> ReceivedMessages => _receivedMessages;
    private readonly List<string> _receivedMessages = new(100);
    public string LastMessage => _receivedMessages.Any() ? _receivedMessages.Last() : string.Empty;
    private bool HasActivityTimedOut(DateTimeOffset time) => time - LastActivity > NoActivityTimeout;

    public async Task Initialize()
    {
        try
        {
            await TcpConnection.ConnectAsync(ServerEndPoint);
            if (TcpConnection.Connected)
            {
                Stream = TcpConnection.GetStream();
                Logger.LogInformation("WiFred connected to {Server}.", TcpConnection.Client.RemoteEndPoint);
                RunTask = Task.Run(async () => await Run(CancellationTokenSource.Token));
                ReceiveTask = Task.Run(async () => await ReadFromServer(CancellationTokenSource.Token));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection to WiThrottle server failed.");
        }
    }

    public async Task Stop()
    {

        await Disconnect();
        CancellationTokenSource.Cancel();
        //Task.WaitAll(new[] { ReceiveTask!, RunTask! });

    }

    public async Task Run(CancellationToken cancellationToken)
    {
        LastActivity = TimeProvider.UtcNow;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (HasEmptyBattery || HasActivityTimedOut(TimeProvider.UtcNow))
            {
                await SetEmergencyStop();
                Locos.SetAll(LocoState.Inactive);
            }
            if (!TcpConnection.Client.Connected)
            {
                Locos.Update(LocoState.Active, LocoState.ShouldActivate);

            }
            if (IsEmergencyStop && SpeedOfAttachedLocos == 0)
            {
                IsEmergencyStop = false;
            }
            await SendHeartBeat();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }


    private async Task Disconnect()
    {
        Locos.Update(LocoState.Active, LocoState.Deactivating);
        await WriteToServer("Q\n");
        await Task.Delay(500);
        TcpConnection.Close();
    }

    private async Task SendHeartBeat()
    {
        if ((TimeProvider.UtcNow - LastHeartBeat) < KeepAliveTimeout) return;
        await WriteToServer("*\n");
        LastHeartBeat = TimeProvider.UtcNow;
        LastActivity = TimeProvider.UtcNow;
    }

    private async Task SetEmergencyStop()
    {
        await WriteToServer("MTA*<;>X\n");
    }

    private async Task WriteToServer(string command)
    {
        var bytes = Encoding.UTF8.GetBytes(command);
        try
        {
            await Stream.WriteAsync(bytes);
            await Stream.FlushAsync();
            LastActivity = TimeProvider.UtcNow;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Writing '{Command}' to server failed.", command.Replace("\n", ""));
        }
    }

    private async Task ReadFromServer(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var delimiters = new char[]{ '\r', '\n' };
        while (!cancellationToken.IsCancellationRequested)
        {
            if (TcpConnection.Connected && TcpConnection.Available > 0)
            {
                try
                {
                    var bytesRead = await Stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead > 0)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var messages = data.Split(delimiters).Where(m => m.Length>0);
                        _receivedMessages.AddRange(messages);
                        LastActivity = TimeProvider.UtcNow;
                        Logger.LogDebug("Received {message}", string.Join("|", messages));
                    }

                }
                catch (Exception ex)
                {

                    Logger.LogError(ex, "Read from server failed.");
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                TcpConnection?.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal class Locos
{
    const int NumberOfHandledLocos = 4;
    private readonly LocoState[] States = new LocoState[NumberOfHandledLocos];
    public void SetAll(LocoState state) => States.SetAll(state);
    public void Update(LocoState oldState, LocoState newState) => States.SetWhen(newState, s => s == oldState);
    public bool AreAllInactive => States.All(s => s == LocoState.Inactive);
}

public enum LocoState
{
    Inactive = 0,
    ShouldActivate = 1,
    Active = 2,
    Deactivating = 3,
    EnterFunctions = 4,
    LeaveFunctions = 5
}

public enum ThrottleState
{
    StartingUp,
    Connecting,
    Connected,
    Disconnecting,
    Disconnected
}

public record WiFredSimulatorSettings(IPEndPoint ServerEndPoint, TimeSpan KeepAliveTimeout, TimeSpan NoActivityTimeout);
