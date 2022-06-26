namespace Tellurian.Trains.LocoNetMonitor;
internal class SlotTable
{
    readonly Dictionary<byte, Slot> _table = new(127);
    readonly ILocoOwnerService _locoOwnerService;
    readonly LocoNetInterface _locoNetInterface;

    public SlotTable(ILocoOwnerService locoOwnerService, LocoNetInterface locoNetInterface)
    {
        _locoOwnerService = locoOwnerService;
        _locoNetInterface = locoNetInterface;
        for (byte slot = 0; slot < 128; slot++) _table.Add(slot, new Slot(slot));
    }

    public Slot this[byte i] { get { return _table[i]; } }
    public bool BlockUnassignedAdresses { get; set; }
    public IEnumerable<Slot> Slots => _table.Values;

    public async Task<byte> Update(byte[] loconetData, CancellationToken stoppingToken)
    {
        if (loconetData == null || loconetData.Length == 0) return 0;
        var slotNumber = GetSlotNumber(loconetData);
        if (!slotNumber.IsLocoSlot()) return 0;
        var slot = _table[slotNumber];
        slot.Update(loconetData);
        if (slot.HasNoAddress) await RequestSlot(slotNumber, stoppingToken);
        else if (slot.HasNoOwner) slot.Owner = _locoOwnerService.GetOwner(slot.Address);
        if (BlockUnassignedAdresses && slot.HasNoOwner && slot.Speed > 1) await SetSlotSpeedZero(slotNumber, stoppingToken);
        return slotNumber;

        static byte GetSlotNumber(byte[] loconetData)
        {
            return loconetData[0] switch
            {
                (>= 0xA0) and (<= 0xA3) => loconetData[1],
                0xD4 => loconetData[2],
                0xE7 or 0xEF => loconetData[2],
                _ => 0
            };
        }
    }

    private async Task RequestSlot(byte slotNumber, CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested )
        {
            var data = new byte[] { 0xBB, slotNumber, 0x00 }.AppendChecksum();
            await _locoNetInterface.Write(data, stoppingToken);
        }
    }

    private async Task SetSlotSpeedZero(byte slotNumber, CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            var data = new byte[] { 0xA0, slotNumber, 0x00 }.AppendChecksum();
            await _locoNetInterface.Write(data, stoppingToken);
        }
    }
}

