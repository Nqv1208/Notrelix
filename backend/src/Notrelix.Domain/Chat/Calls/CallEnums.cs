namespace Notrelix.Domain.Chat.Calls;

/// <summary>Voice or video (SYS-ADR-002 decision 6).</summary>
public enum CallKind
{
    Voice,
    Video,
}

/// <summary>Ringing, then Active, then Ended (spec section 4). Ended is final.</summary>
public enum CallState
{
    Ringing,
    Active,
    Ended,
}

/// <summary>Why a call ended. Set only with <see cref="CallState.Ended"/>.</summary>
public enum CallEndReason
{
    Declined,
    Missed,
    Cancelled,
    Busy,
    Failed,
    Completed,
}

/// <summary>The persisted, snake_case spelling of the call enums. The spelling is part of the data contract.</summary>
public static class CallWire
{
    public static string ToWire(this CallKind kind) => kind switch
    {
        CallKind.Voice => "voice",
        CallKind.Video => "video",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown call kind."),
    };

    public static CallKind CallKindFromWire(string value) => value switch
    {
        "voice" => CallKind.Voice,
        "video" => CallKind.Video,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown call kind."),
    };

    public static string ToWire(this CallState state) => state switch
    {
        CallState.Ringing => "ringing",
        CallState.Active => "active",
        CallState.Ended => "ended",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown call state."),
    };

    public static CallState CallStateFromWire(string value) => value switch
    {
        "ringing" => CallState.Ringing,
        "active" => CallState.Active,
        "ended" => CallState.Ended,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown call state."),
    };

    public static string ToWire(this CallEndReason reason) => reason switch
    {
        CallEndReason.Declined => "declined",
        CallEndReason.Missed => "missed",
        CallEndReason.Cancelled => "cancelled",
        CallEndReason.Busy => "busy",
        CallEndReason.Failed => "failed",
        CallEndReason.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown call end reason."),
    };

    public static CallEndReason CallEndReasonFromWire(string value) => value switch
    {
        "declined" => CallEndReason.Declined,
        "missed" => CallEndReason.Missed,
        "cancelled" => CallEndReason.Cancelled,
        "busy" => CallEndReason.Busy,
        "failed" => CallEndReason.Failed,
        "completed" => CallEndReason.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown call end reason."),
    };
}
