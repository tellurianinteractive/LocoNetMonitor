using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.LocoNetMonitor.Slots;
internal class SlotTableUpdater : BackgroundService
{
    private readonly ILogger _logger;
    readonly IOptions<AppSettings> _options;

    readonly SlotTable _slots;

    AppSettings Settings => _options.Value;

    public SlotTableUpdater(IOptions<AppSettings> options, SlotTable slots, ILogger<SlotTableUpdater> logger)
    {
        _options = options;
        _logger = logger;
        _slots = slots;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{service} is starting.", nameof(SlotTableUpdater));
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
                if (slotNumber > 0) _logger.LogInformation("Updated: {slot}", _slots[slotNumber].ToString());
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
        _logger.LogInformation("{service} is stopping.", nameof(SlotTableUpdater));
    }
}