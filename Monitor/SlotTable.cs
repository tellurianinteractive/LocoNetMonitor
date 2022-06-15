using System.Diagnostics.CodeAnalysis;

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
    public Action<byte[]>? SendToLocoNet { get; set; }

    public IEnumerable<Slot> Slots => _table.Values;

    public async Task<byte> Update(byte[] loconetData, CancellationToken stoppingToken)
    {
        if (loconetData == null || loconetData.Length == 0) return 0;
        var slotNumber = GetSlotNumber(loconetData);
        if (slotNumber == 0) return 0;

        var slot = _table[slotNumber];
        switch (loconetData[0])
        {
            case 0xA0:
                slot.Speed = loconetData[2];
                break;
            case 0xA1:
                slot.DirectionAndFunctionsF0_F4 = loconetData[2];
                break;
            case 0xA2:
                slot.FunctionsF5_F8 = loconetData[2];
                break;
            case 0xA3:
                slot.FunctionsF9_F12 = loconetData[2];
                break;
            case 0xD4:
                if (loconetData[1] != 0x20) break;
                switch (loconetData[3])
                {
                    case 0x08:
                        slot.FunctionsF13_F19 = loconetData[4];
                        break;
                    case 0x09:
                        slot.FunctionsF21_F27 = loconetData[4];
                        break;
                    case 0x05:
                        slot.FunctionsF20AndF28 = loconetData[4];
                        break;
                    default:
                        break;
                }
                break;
            case 0xE7:
            case 0xEF:
                if (loconetData.Length < 10 || loconetData[1] != 0x0E) break;
                slotNumber = loconetData[2];
                slot.Status = loconetData[3];
                slot.Speed = loconetData[5];
                slot.DirectionAndFunctionsF0_F4 = loconetData[6];
                slot.FunctionsF5_F8 = loconetData[10];
                slot.SetAddress(loconetData[9], loconetData[4]);
                break;
            default:
                return 0;
        }
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
        if (!stoppingToken.IsCancellationRequested &&  CanSendToLocoNet(slotNumber))
        {
            var data = new byte[] { 0xBB, slotNumber, 0x00 }.AppendChecksum();
            await _locoNetInterface.Write(data, stoppingToken);
        }
    }

    private async Task SetSlotSpeedZero(byte slotNumber, CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested && CanSendToLocoNet(slotNumber))
        {
            var data = new byte[] { 0xA0, slotNumber, 0x00 }.AppendChecksum();
            await _locoNetInterface.Write(data, stoppingToken);
        }
    }

    [MemberNotNullWhen(true, nameof(SendToLocoNet))]
    bool CanSendToLocoNet(byte slotNumber) => SendToLocoNet is not null && slotNumber.IsLocoSlot();
}

