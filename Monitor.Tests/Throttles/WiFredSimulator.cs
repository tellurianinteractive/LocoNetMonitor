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
internal class WiFredSimulator
{
    public WiFredSimulator(IPEndPoint serverEndPoint, ILogger logger)
    {
        Locos = new();
        TcpConnection = new TcpClient();
        State = ThrottleState.StartingUp;
        ServerEndPoint = serverEndPoint;
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
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromMilliseconds(5000);
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan NoActivityTimeout { get; set; } = TimeSpan.FromHours(1);

    private readonly IPEndPoint ServerEndPoint;
    private readonly TcpClient TcpConnection;
    private readonly Locos Locos;
    private readonly ILogger Logger;

    private ThrottleState State;
    private DateTimeOffset LastHeartBeat;
    private DateTimeOffset LastSpeedUpdate;
    private DateTimeOffset LastActivity;

    private byte SpeedOfAttachedLocos { get; set; }
    private bool IsReversing;
    private bool HasActivityTimedOut(DateTimeOffset time) => time - LastActivity > NoActivityTimeout;

    public async Task Initialize()
    {
        try
        {
            await TcpConnection.ConnectAsync(ServerEndPoint);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection to WiThrottle server failed.");
            throw;
        }
    }

    public async Task Run()
    {
        while (true)
        {
            var now = DateTimeOffset.Now;
            if (HasEmptyBattery || HasActivityTimedOut(now))
            {
                await SetEmergencyStop();
                Locos.SetAll(LocoState.Inactive);
            }
            if (Locos.AreAllInactive)
            {
                await Disconnect();
                return; // Exits the loop
            }
            if (!TcpConnection.Client.Connected)
            {
                Locos.Update(LocoState.Active, LocoState.ShouldActivate);

            }
            if (IsEmergencyStop && SpeedOfAttachedLocos == 0)
            {
                IsEmergencyStop = false;
            }
            await Task.Delay(KeepAliveTimeout);
        }
    }


    private async Task Disconnect()
    {
        Locos.Update(LocoState.Active, LocoState.ShouldActivate);
        await WriteToServer("Q\n");
        await Task.Delay(1000);
        TcpConnection.Close();
    }

    private async Task SendHeartBeat()
    {
        await WriteToServer("");
    }

    private async Task SetEmergencyStop()
    {
        await WriteToServer("MTA*<;>X\n");
    }

    private async Task WriteToServer(string command)
    {
        var bytes = Encoding.UTF8.GetBytes(command);
        await TcpConnection.GetStream().WriteAsync(bytes);
    }

    private void ReadFromServer()
    {
        if (TcpConnection.Connected && TcpConnection.Available > 0)
        {
        }
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

}
