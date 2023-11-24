using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Slots;
internal class SlotTableUpdater(IOptions<AppSettings> options, ISlotTable slots, ILogger<SlotTableUpdater> logger) : BackgroundService
{
    private readonly ILogger _logger = logger;
    readonly IOptions<AppSettings> _options = options;
    readonly ISlotTable _slots = slots;

    AppSettings Settings => _options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{BackgroundService} is starting.", nameof(SlotTableUpdater));
        var listenEndpoint = new IPEndPoint(IPAddress.Any, Settings.UdpBroadcaster.LocalPortNumber);
    Restart:
        try
        {
            using var udpClient = new UdpClient();
            udpClient.JoinMulticastGroup(Settings.MulticastIPAddress());
            udpClient.Client.Bind(listenEndpoint);
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var slotNumber = await _slots.Update(result.Buffer, stoppingToken);
                if (slotNumber > 0) _logger.LogInformation("Updated: {Slot}", _slots[slotNumber].ToString());
            }

        }
        catch (OperationCanceledException)
        {
            // Ignore and fall through.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slot table updater failed: Exception type {type}.", ex.GetType().Name);
            goto Restart;
        }
        _logger.LogInformation("{BackgroundService} is stopping.", nameof(SlotTableUpdater));
    }
}