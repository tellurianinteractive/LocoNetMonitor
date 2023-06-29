using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tellurian.Trains.LocoNetMonitor.Slots;

namespace Tellurian.Trains.LocoNetMonitor.Tests.Throttles;
internal class TestSlotTable : ISlotTable
{
    const int MaxSlots = 127;
    public TestSlotTable() {
        for (byte i = 0; i < MaxSlots; i++) { Slots[i] = new Slot(i); }
    }
    public Slot[] Slots = new Slot[MaxSlots];
    Slot ISlotTable.this[byte i] => throw new NotImplementedException();

    IEnumerable<Slot> ISlotTable.Slots => throw new NotImplementedException();

    Task ISlotTable.BlockLocoFromDriving(Slot slot, CancellationToken stoppingToken) => throw new NotImplementedException();
    Task ISlotTable.DispatchSlot(Slot slot, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    IEnumerable<Slot> ISlotTable.FindByIPAddress(IPAddress address) => throw new NotImplementedException();
    IEnumerable<Slot> ISlotTable.FindByLocoAddress(int address) => Slots.Where(s => s.Address == address).ToList();
    byte ISlotTable.GetSlotNumber(byte[] loconetData) => throw new NotImplementedException();
    Task ISlotTable.RequestAddress(int locoAddress, CancellationToken stoppingToken)
    {
        var freeSlot = Slots.First(s => s.Address == 0 && s.IsFree);
        freeSlot.SetAddress(locoAddress);
        freeSlot.Status &= 0xFF;
        return Task.CompletedTask;
    }

    Task ISlotTable.RequestSlot(Slot slot, CancellationToken stoppingToken) => throw new NotImplementedException();
    Task ISlotTable.SetSlotActive(Slot slot, CancellationToken stoppingToken)
    {
        Slots[slot.Number].Status &= 0xFF;
        return Task.CompletedTask;
    }

    Task ISlotTable.SetSlotDirection(Slot slot, bool isForward, CancellationToken stoppingToken)
    {
        Slots[slot.Number].SetDirection(isForward);
        return Task.CompletedTask;
    }

    Task ISlotTable.SetSlotFunction(Slot slot, int function, bool setOn, CancellationToken stoppingToken)
    {
        Slots[slot.Number].SetFunction(function, setOn);
        return Task.CompletedTask;
    }

    Task ISlotTable.SetSlotInactive(Slot slot, CancellationToken stoppingToken)
    {
        Slots[slot.Number].Status &= 0xCF;
        return Task.CompletedTask;
    }

    Task ISlotTable.SetSlotSpeed(Slot slot, byte speed, CancellationToken stoppingToken)
    {
        Slots[slot.Number].Speed = speed;
        return Task.CompletedTask;
    }

    Task<byte> ISlotTable.Update(byte[] loconetData, CancellationToken stoppingToken) => throw new NotImplementedException();
}
