using System.Net;

namespace Tellurian.Trains.LocoNetMonitor.Slots;
internal interface ISlotTable
{
    Slot this[byte i] { get; }

    IEnumerable<Slot> Slots { get; }

    Task BlockLocoFromDriving(Slot slot, CancellationToken stoppingToken);
    Task DispatchSlot(Slot slot, CancellationToken stoppingToken);
    IEnumerable<Slot> FindByIPAddress(IPAddress address);
    IEnumerable<Slot> FindByLocoAddress(int address);
    byte GetSlotNumber(byte[] loconetData);
    Task RequestAddress(int locoAddress, CancellationToken stoppingToken);
    Task RequestSlot(Slot slot, CancellationToken stoppingToken);
    Task SetSlotActive(Slot slot, CancellationToken stoppingToken);
    Task SetSlotDirection(Slot slot, bool isForward, CancellationToken stoppingToken);
    Task SetSlotFunction(Slot slot, int function, bool setOn, CancellationToken stoppingToken);
    Task SetSlotInactive(Slot slot, CancellationToken stoppingToken);
    Task SetSlotSpeed(Slot slot, byte speed, CancellationToken stoppingToken);
    Task<byte> Update(byte[] loconetData, CancellationToken stoppingToken);

}