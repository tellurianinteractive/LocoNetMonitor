using System.Text;

namespace Tellurian.Trains.LocoNetMonitor;

public record Slot(byte Number)
{
    public string? Owner { get; set; }
    public short Address { get; private set; }
    public void SetAddress(byte highOrder, byte lowOrder) { Address = GetAddress(highOrder, lowOrder); }
    public byte Speed { get; set; }
    public byte Status { private get; set; }
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
        text.AppendLine($"Functions: {string.Join(',', Functions.Select((f, i) => (f, i)).Where(x => x.f).Select(x => $"F{x.i}"))}");
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

