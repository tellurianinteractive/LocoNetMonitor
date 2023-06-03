using System.Diagnostics.CodeAnalysis;

namespace Tellurian.Trains.LocoNetMonitor.Throttles;
public abstract record Command
{
    protected const string Delimiter = "<;>";
    public static bool TryParse(string line, [NotNullWhen(true)] out Command? entry)
    {
        var text = line.Trim('\r', '\n', ' ');
        entry = null;
        var type = GetEntryType(text);
        if (type is null) return false;
        entry = (Command?)Activator.CreateInstance(type);
        entry?.Init(text.Split(Delimiter));
        return entry is not null;
    }

    protected abstract void Init(string[] fields);
    public virtual string? Reply => null;

    [MemberNotNullWhen(true, nameof(Reply))]
    public bool HasReply => !string.IsNullOrWhiteSpace(Reply);


    private static Type? GetEntryType(string message)
    {
        var mapping = EntryMappings
            .SingleOrDefault((em) => IsMatch(message, em.Key));
        if (mapping.Key is null) return null;
        return mapping.Value.Name switch
        {
            nameof(DispatchOrReleaseCommand) when message[^1] == 'd' => typeof(DispatchCommand),
            nameof(DispatchOrReleaseCommand) when message[^1] == 'r' => typeof(ReleaseCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "V") => typeof(SpeedCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "F") => typeof(FunctionCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "f") => typeof(FunctionCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "R") => typeof(DirectionCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "q") => typeof(QueryCommand),
            nameof(ActionCommand) when message.Contains(Delimiter + "I") => typeof(Idle),
            _ => mapping.Value
        };
    }


    private static readonly IDictionary<string, Type> EntryMappings = new Dictionary<string, Type>()
    {
        {NameCommand.Prefix, typeof(NameCommand) },
        {IdCommand.Prefix, typeof(IdCommand) },
        {Heartbeat.Prefix, typeof(Heartbeat) },
        {DispatchOrReleaseCommand.Prefix, typeof(DispatchOrReleaseCommand) },
        {ActionCommand.Prefix, typeof(ActionCommand) },
        {AssignCommand.Prefix, typeof(AssignCommand) },
    };

    private static bool IsMatch(string message, string pattern)
    {
        for (var i = 0; i < pattern.Length; i++)
        {
            if (message[i] != pattern[i] && pattern[i] != '_') return false;
        }
        return true;
    }
}


public sealed record NameCommand : Command
{
    public const string Prefix = "N";
    override protected void Init(string[] fields) { Name = fields[0][Prefix.Length..]; }
    public string Name { get; private set; } = string.Empty;
}
public sealed record IdCommand : Command
{
    public const string Prefix = "HU";
    override protected void Init(string[] fields) { Id = fields[0][Prefix.Length..]; }

    public string Id { get; private set; } = string.Empty;
}

public sealed record Heartbeat : Command
{
    public const string Prefix = "*";
    override protected void Init(string[] fields)
    {
        if (fields[0].Length > 1) Timeout = int.Parse(fields[0][Prefix.Length..]);
    }
    public int Timeout { get; private set; }
}

public abstract record DispatchOrReleaseCommand : Command
{
    public const string Prefix = "M_-";
    override protected void Init(string[] fields)
    {
        ThrottleId = fields[0][1];
        All = fields[0][3] == '*';
        Key = All ? null : fields[0][(Prefix.Length)..];
    }
    public char ThrottleId { get; private set; }
    public string? Key { get; private set; }
    public bool All { get; private set; }

    public override string? Reply => string.IsNullOrWhiteSpace(Key) ? $"M{ThrottleId}-*{Delimiter}" : $"M{ThrottleId}-{Key}{Delimiter}";
}


public sealed record DispatchCommand : DispatchOrReleaseCommand
{
}

public sealed record ReleaseCommand : DispatchOrReleaseCommand
{
}

public sealed record AssignCommand : Command
{
    public const string Prefix = "M_+";
    public char ThrottleId { get; private set; }
    public int Address { get; private set; }
    public string? Key { get; private set; }

    protected override void Init(string[] fields)
    {
        if (fields.Length != 2) throw new InvalidOperationException(nameof(fields));
        ThrottleId = fields[0][1];
        Key = fields[0][3..];
        Address = short.Parse(fields[1][1..]);
    }

    public override string? Reply => $"M{ThrottleId}+{Key}{Delimiter}";
}

public abstract record ActionCommand : Command
{
    public const string Prefix = "M_A";
    public bool All { get; protected set; }
    public string? Key { get; protected set; }

    protected override void Init(string[] fields)
    {
        All = fields[0][3] == '*';
        Key = All ? null : fields[0][3..];
    }
}

public sealed record FunctionCommand : ActionCommand
{
    public int Function { get; private set; }
    public bool IsOn { get; private set; }
    public bool IsPush { get; private set; }
    protected override void Init(string[] fields)
    {
        Function = int.Parse(fields[1][2..]);
        IsOn = fields[1][1] == '1';
        IsPush = fields[1][0] == 'F';
        base.Init(fields);
    }
}

public sealed record SpeedCommand : ActionCommand
{
    public static SpeedCommand Create(string? key, byte speed) =>
        new()
        {
            Key = key,
            All = key is null,
            Speed = speed
        };


    public byte Speed { get; private set; }
    protected override void Init(string[] fields)
    {
        Speed = byte.Parse(fields[1][1..]);
        base.Init(fields);
    }
}

public sealed record DirectionCommand : ActionCommand
{
    public bool IsForward { get; private set; }
    protected override void Init(string[] fields)
    {
        IsForward = fields[1][1] == '1';
        base.Init(fields);
    }
}

public sealed record Idle : ActionCommand
{

}

public sealed record QueryCommand : ActionCommand
{
    protected override void Init(string[] fields) => throw new NotImplementedException();
}

