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
    private string Name => Settings.ThrottleName;
    private string Id => "001122334455";

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
    private bool IsConnectedToServer => TcpConnection.Connected;

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
        await Task.Delay(1000);
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
            if ((TimeProvider.UtcNow - LastHeartBeat) >= KeepAliveTimeout) await SendHeartBeat();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    private async Task Disconnect()
    {
        Locos.Update(LocoState.Active, LocoState.Deactivating);
        await WriteToServer("Q");
        await Task.Delay(500);
        TcpConnection.Close();
    }

    private async Task SendHeartBeat()
    {
        await WriteToServer("*");
        LastHeartBeat = TimeProvider.UtcNow;
        LastActivity = TimeProvider.UtcNow;
    }

    private async Task RegisterThrottle()
    {
        await WriteToServer($"N{Name}");
        await WriteToServer($"HU{Id}");
    }

    private async Task SetEmergencyStop()
    {
        await WriteToServer("MTA*<;>X");
    }

    private async Task WriteToServer(string command)
    {
        if (IsConnectedToServer)
        {
            var bytes = GetBytes(command);
            try
            {
                await Stream.WriteAsync(bytes);
                await Stream.FlushAsync();
                LastActivity = TimeProvider.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Writing '{Command}' to server failed.", command);
            }
        }
        else
        {
            Logger.LogError("Writing '{Command}' to server failed because not connected to server.", command);
        }

        static byte[] GetBytes(string command) => Encoding.UTF8.GetBytes(command + '\n');
    }

    private async Task ReadFromServer(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var delimiters = new char[] { '\r', '\n' };
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
                        var messages = data.Split(delimiters).Where(m => m.Length > 0);
                        if (IsAnyTimeout(messages)) await SendHeartBeat();
                        _receivedMessages.AddRange(messages);
                        if (_receivedMessages.Count == 4) await RegisterThrottle();
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

        static bool IsAnyTimeout(IEnumerable<string> messages) => messages.Any(m => m == "*");
    }

    #region Dispose
    private bool disposedValue;
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
    #endregion Dispose
}

internal class Locos
{
    const int NumberOfHandledLocos = 4;
    private readonly LocoState[] States = new LocoState[NumberOfHandledLocos];
    public void SetAll(LocoState state) => States.SetAll(state);
    public void Update(LocoState oldState, LocoState newState) => States.SetWhen(newState, s => s == oldState);
    public bool AreAllInactive => States.All(s => s == LocoState.Inactive);

    private uint Functions = 0;

    public async Task Update(Func<string, Task> write)
    {
        for (var loco = 0; loco < States.Length; loco++)
        {
            if (States[loco] == LocoState.SetFunctions)
            {

            }
        }
    }

    //private async Task SetFunction(int loco)
}

public enum LocoState
{
    Inactive = 0,
    ShouldActivate = 1,
    Active = 2,
    Deactivating = 3,
    GetFunctions = 4,
    SetFunctions = 5
}

public enum ThrottleState
{
    StartingUp,
    Connecting,
    Connected,
    Disconnecting,
    Disconnected
}

public record WiFredSimulatorSettings(string ThrottleName, IPEndPoint ServerEndPoint, TimeSpan KeepAliveTimeout, TimeSpan NoActivityTimeout);
