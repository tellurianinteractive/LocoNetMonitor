using System.Net;

namespace Tellurian.Trains.LocoNetMonitor.Slots;
internal class SlotTable : ISlotTable
{
    private const byte MaxSlots = 120;
    private readonly Dictionary<byte, Slot> _table = new(MaxSlots);
    private readonly IOptions<AppSettings> _options;
    private readonly ILocoOwnerService _locoOwnerService;
    private readonly ISerialPortGateway _locoNetGateway;
    private readonly ILogger<SlotTable> _logger;

    public SlotTable(IOptions<AppSettings> options, ILocoOwnerService locoOwnerService, ISerialPortGateway locoNetGateway, ILogger<SlotTable> logger)
    {
        _options = options;
        _logger = logger;
        _locoOwnerService = locoOwnerService;
        _locoNetGateway = locoNetGateway;
        for (byte slotNumber = 0; slotNumber < MaxSlots; slotNumber++) _table.Add(slotNumber, new Slot(slotNumber));
    }

    private SlotTableSettings Settings => _options.Value.SlotTable;

    public Slot this[byte i] { get { return _table[i]; } }
    public IEnumerable<Slot> Slots => _table.Values;

    public async Task<byte> Update(byte[] loconetData, CancellationToken stoppingToken)
    {
        if (loconetData == null || loconetData.Length == 0) return 0;
        var slotNumber = GetSlotNumber(loconetData);
        if (!slotNumber.IsLocoSlot()) return 0;
        var slot = _table[slotNumber];
        slot.Update(loconetData);
        if (slot.HasNoAddress) await RequestSlot(slot, stoppingToken);
        else if (slot.HasNoOwner)
        {
            slot.Owner = _locoOwnerService.GetOwner(slot.Address);
            if (slot.HasOwner)
                _logger.LogInformation("Slot {number} with address {address} was assigned to {owner}.", slot.Number, slot.Address, slot.Owner);
        }
        if (slot.Address > 0 && Settings.BlockDrivingForUnassignedAdresses && slot.HasNoOwner && slot.Speed > 1) await BlockLocoFromDriving(slot, stoppingToken);
        else if (slot.Usage <= Usage.Idle) await SetSlotActive(slot, stoppingToken);
        return slotNumber;
    }

    public IEnumerable<Slot> FindByLocoAddress(int address) => _table.Values.Where(s => s.Address == address);
    public IEnumerable<Slot> FindByIPAddress(IPAddress address) => _table.Values.Where(s => s.IPAddress == address);

    public byte GetSlotNumber(byte[] loconetData)
    {
        return loconetData[0] switch
        {
            >= 0xA0 and <= 0xA3 => loconetData[1],
            0xD4 => loconetData[2],
            0xE7 or 0xEF => loconetData[2],
            _ => 0
        };
    }

    public async Task SetSlotActive(Slot slot, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xBA, slot.Number, slot.Number }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task RequestSlot(Slot slot, CancellationToken stoppingToken)
    {
        _logger.LogDebug("Requesting slot {number}", slot.Number);
        var data = new byte[] { 0xBB, slot.Number, 0x00 }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task RequestAddress(int locoAddress, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xBF, (byte)(locoAddress >> 7), (byte)(locoAddress & 0xFF) }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task SetSlotInactive(Slot slot, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xB5, slot.Number, (byte)(slot.Status & 0x87) }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task DispatchSlot(Slot slot, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xBA, slot.Number, 0x00 }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public Task BlockLocoFromDriving(Slot slot, CancellationToken stoppingToken)
    {
        _logger.LogWarning("Loco {loco} is blocked from driving. Please, reserve the address", slot.Address);
        return SetSlotSpeed(slot, 0, stoppingToken);
    }

    public async Task SetSlotSpeed(Slot slot, byte speed, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xA0, slot.Number, speed }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task SetSlotDirection(Slot slot, bool isForward, CancellationToken stoppingToken)
    {
        var data = new byte[] { 0xA1, slot.Number, slot.DirectionAndFunctionsF0_F4WithNewDirection(isForward) }.AppendChecksum();
        await _locoNetGateway.Write(data, stoppingToken);
    }

    public async Task SetSlotFunction(Slot slot, int function, bool setOn, CancellationToken stoppingToken)
    {
        var data = Array.Empty<byte>(); // TODO: Implement function setting on or off.
        if (data.Length > 0)
        {
            await _locoNetGateway.Write(data, stoppingToken);
            if (setOn) _logger.LogDebug("Set F{function} for loco {loco}.", function, slot.Address);
            else _logger.LogDebug("Clear F{function} for loco {loco}.", function, slot.Address);
        }
    }
}

