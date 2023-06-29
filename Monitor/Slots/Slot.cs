using System.Net;
using System.Text;

namespace Tellurian.Trains.LocoNetMonitor.Slots;

public record Slot(byte Number)
{
    public string? CommandStationId { get; set; }
    public short Address { get; private set; }
    public IPAddress? IPAddress { get; set; }
    public string Id { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string? Name { get; set; }
    public void SetAddress(byte highOrder, byte lowOrder)
    {
        Address = GetAddress(highOrder, lowOrder);
    }
    public void SetAddress(int address)
    {
        if (address < 1 || address > 9999) throw new ArgumentOutOfRangeException(nameof(address), address.ToString());
        Address = (short)address;
    }
    public byte Speed { get; set; }
    public byte Status { get; set; }
    public byte DirectionAndFunctionsF0_F4 { private get; set; }
    public byte FunctionsF5_F8 { private get; set; }
    public byte FunctionsF9_F12 { private get; set; }
    public byte FunctionsF13_F19 { private get; set; }
    public byte FunctionsF20AndF28 { private get; set; }
    public byte FunctionsF21_F27 { private get; set; }
    public byte SpeedSteps => (byte)(IsTrue(Status & 0x03) ? 128 : IsTrue(Status & 0x01) ? 28 : IsTrue(Status & 0x02) ? 14 : 28);
    public bool IsAdvancedConsistAllowed => IsTrue(Status & 0x04);
    public Usage Usage => HasNoAddress ? Usage.Unknown : IsTrue(Status & 0x30) ? Usage.Active : IsTrue(Status & 0x20) ? Usage.Idle : IsTrue(Status & 0x10) ? Usage.Common : Usage.Free;
    public Consist Consist => IsTrue(Status & 0x48) ? Consist.Mid : IsTrue(Status & 0x40) ? Consist.Top : IsTrue(Status & 0x08) ? Consist.Sub : Consist.None;
    public Direction Direction => HasNoAddress ? Direction.Unknown : IsTrue(DirectionAndFunctionsF0_F4 & 0x20) ? Direction.Backward : Direction.Forward;
    public bool HasNoOwner => string.IsNullOrWhiteSpace(Owner);
    public bool HasOwner => !HasNoOwner;
    public bool HasNoAddress => !HasAddress;
    public bool HasAddress => Address > 0;
    public bool IsFree => (Status & 0x30) == 0;

    public bool IsBlocked { get; set; }

    public byte DirectionAndFunctionsF0_F4WithNewDirection(bool isForward) => (byte)(isForward ? DirectionAndFunctionsF0_F4 | 0x20 : DirectionAndFunctionsF0_F4 & 0x1F);
    public void Update(byte[] loconetData)
    {
        switch (loconetData[0])
        {
            case 0xA0:
                Speed = loconetData[2];
                break;
            case 0xA1:
                DirectionAndFunctionsF0_F4 = loconetData[2];
                break;
            case 0xA2:
                FunctionsF5_F8 = loconetData[2];
                break;
            case 0xA3:
                FunctionsF9_F12 = loconetData[2];
                break;
            case 0xD4:
                if (loconetData[1] != 0x20) break;
                switch (loconetData[3])
                {
                    case 0x08:
                        FunctionsF13_F19 = loconetData[4];
                        break;
                    case 0x09:
                        FunctionsF21_F27 = loconetData[4];
                        break;
                    case 0x05:
                        FunctionsF20AndF28 = loconetData[4];
                        break;
                    default:
                        break;
                }
                break;
            case 0xE7:
            case 0xEF:
                if (loconetData.Length < 10 || loconetData[1] != 0x0E) break;
                Status = loconetData[3];
                Speed = loconetData[5];
                DirectionAndFunctionsF0_F4 = loconetData[6];
                FunctionsF5_F8 = loconetData[10];
                SetAddress(loconetData[9], loconetData[4]);
                break;
            default:
                return;
        }
    }

    public IEnumerable<bool> Functions => new[]
    {
        IsTrue(DirectionAndFunctionsF0_F4 & 0x10),
        IsTrue(DirectionAndFunctionsF0_F4 & 0x01),
        IsTrue(DirectionAndFunctionsF0_F4 & 0x02),
        IsTrue(DirectionAndFunctionsF0_F4 & 0x04),
        IsTrue(DirectionAndFunctionsF0_F4 & 0x08),
        IsTrue(FunctionsF5_F8 & 0x01),
        IsTrue(FunctionsF5_F8 & 0x02),
        IsTrue(FunctionsF5_F8 & 0x04),
        IsTrue(FunctionsF5_F8 & 0x08),
        IsTrue(FunctionsF9_F12 & 0x01),
        IsTrue(FunctionsF9_F12 & 0x02),
        IsTrue(FunctionsF9_F12 & 0x04),
        IsTrue(FunctionsF9_F12 & 0x08),
        IsTrue(FunctionsF13_F19 & 0x01),
        IsTrue(FunctionsF13_F19 & 0x02),
        IsTrue(FunctionsF13_F19 & 0x04),
        IsTrue(FunctionsF13_F19 & 0x08),
        IsTrue(FunctionsF13_F19 & 0x10),
        IsTrue(FunctionsF13_F19 & 0x20),
        IsTrue(FunctionsF13_F19 & 0x40),
        IsTrue(FunctionsF20AndF28 & 0x20),
        IsTrue(FunctionsF21_F27 & 0x01),
        IsTrue(FunctionsF21_F27 & 0x02),
        IsTrue(FunctionsF21_F27 & 0x04),
        IsTrue(FunctionsF21_F27 & 0x08),
        IsTrue(FunctionsF21_F27 & 0x10),
        IsTrue(FunctionsF21_F27 & 0x20),
        IsTrue(FunctionsF21_F27 & 0x40),
        IsTrue(FunctionsF20AndF28 & 0x40),
    };

    public void SetFunction(int function, bool setOn)
    {
        if (setOn)
        {
            if (function <= 4) DirectionAndFunctionsF0_F4 |= AsOnBitInByte(function);
            else if (function <= 8) FunctionsF5_F8 |= AsOnBitInByte(function);
            else if (function <= 12) FunctionsF9_F12 |= AsOnBitInByte(function);
            else if (function <= 19) FunctionsF13_F19 |= AsOnBitInByte(function);
            else if (function <= 20) FunctionsF20AndF28 |= AsOnBitInByte(function);
            else if (function <= 27) FunctionsF21_F27 |= AsOnBitInByte(function);
            else if (function <= 28) FunctionsF20AndF28 |= AsOnBitInByte(function);
        }
        else
        {
            if (function <= 4) DirectionAndFunctionsF0_F4 &= AsOffBitInByte(function);
            else if (function <= 8) FunctionsF5_F8 &= AsOffBitInByte(function);
            else if (function <= 12) FunctionsF9_F12 &= AsOffBitInByte(function);
            else if (function <= 19) FunctionsF13_F19 &= AsOffBitInByte(function);
            else if (function <= 20) FunctionsF20AndF28 &= AsOffBitInByte(function);
            else if (function <= 27) FunctionsF21_F27 &= AsOffBitInByte(function);
            else if (function <= 28) FunctionsF20AndF28 &= AsOffBitInByte(function);
        }
    }

    private byte AsOffBitInByte(int function) =>
        (byte)~AsOnBitInByte(function);

    private byte AsOnBitInByte(int function) =>
        function switch
        {
            0 => 0x10,
            1 => 0x01,
            2 => 0x02,
            3 => 0x03,
            4 => 0x04,
            5 => 0x01,
            6 => 0x02,
            7 => 0x03,
            8 => 0x04,
            9 => 0x01,
            10 => 0x02,
            11 => 0x03,
            12 => 0x04,
            13 => 0x01,
            14 => 0x02,
            15 => 0x03,
            16 => 0x04,
            17 => 0x10,
            18 => 0x20,
            19 => 0x40,
            20 => 0x20,
            21 => 0x01,
            22 => 0x02,
            23 => 0x03,
            24 => 0x04,
            25 => 0x10,
            26 => 0x20,
            27 => 0x40,
            28 => 0x40,
            _ => 0
        };

    public void SetDirection(bool isForward)
    {
        if (isForward) DirectionAndFunctionsF0_F4 |= 0x20;
        else DirectionAndFunctionsF0_F4 &= 0x1F;
    }

    static bool IsTrue(int? value) => value.HasValue && value > 0;
    static short GetAddress(byte highOrder, byte lowOrder) => (short)(highOrder * 128 + lowOrder);

    string AddressOrUnknown => HasAddress ? Address.ToString() : "Unknown";

    public override string ToString()
    {
        var text = new StringBuilder(200);
        text.Append($"Slot {Number}, ");
        text.Append($"Address {AddressOrUnknown}, ");
        if (HasOwner) text.Append($"Owner: {Owner}, ");
        else text.Append("Unreserved!, ");
        text.Append($"Steps {SpeedSteps}, ");
        text.Append($"Speed {Speed}, ");
        text.Append($"Direction: {Direction}, ");
        text.Append($"Usage: {Usage}, ");
        text.Append($"Functions: {string.Join(',', Functions.Select((f, i) => (f, i)).Where(x => x.f).Select(x => $"F{x.i}"))}");
        return text.ToString();
    }
}

public enum Direction
{
    Unknown,
    Backward,
    Forward,
}
public enum Consist
{
    None,
    Top,
    Mid,
    Sub,
}

public enum Usage
{
    Unknown,
    Free,
    Common,
    Idle,
    Active,
}

