namespace Notrelix.Infrastructure.Events;

public enum DeadLetterAction
{
    Skip,
    Retry,
    PermanentlyDead,
    Notify,
    AutoReplay,
}

public sealed record DeadLetterPolicy
{
    public int MaxRetries { get; init; } = 5;
    public int PoisonThreshold { get; init; } = 5;
    public DeadLetterAction MaxRetriesExceededAction { get; init; } = DeadLetterAction.PermanentlyDead;
    public DeadLetterAction PoisonThresholdExceededAction { get; init; } = DeadLetterAction.PermanentlyDead;
    public bool AutoReplayEnabled { get; init; }
    public TimeSpan AutoReplayDelay { get; init; } = TimeSpan.FromMinutes(5);
    public bool AlertOnDeadLetter { get; init; } = true;
    public string? AlertChannel { get; init; }

    public static readonly DeadLetterPolicy Default = new();

    public DeadLetterAction DetermineAction(int retryCount, int poisonCount)
    {
        if (retryCount >= MaxRetries)
            return MaxRetriesExceededAction;

        if (poisonCount >= PoisonThreshold)
            return PoisonThresholdExceededAction;

        return DeadLetterAction.Retry;
    }

    public TimeSpan GetBackoff(int retryCount) => TimeSpan.FromSeconds(
        Math.Min(Math.Pow(2, retryCount), 60));
}
