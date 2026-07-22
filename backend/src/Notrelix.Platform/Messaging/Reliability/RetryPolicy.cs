namespace Notrelix.Platform.Messaging.Reliability;

public enum BackoffStrategy
{
    Exponential,
    Fixed,
    Linear,
}

public sealed record RetryPolicy
{
    public int MaxRetries { get; init; } = 5;
    public BackoffStrategy Strategy { get; init; } = BackoffStrategy.Exponential;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(60);
    public IReadOnlySet<Type>? RetryableExceptions { get; init; }

    public static readonly RetryPolicy Default = new();

    public bool ShouldRetry(int retryCount, Exception? exception = null)
    {
        if (retryCount >= MaxRetries)
            return false;

        if (exception is not null && RetryableExceptions is not null)
            return RetryableExceptions.Contains(exception.GetType());

        return true;
    }

    public TimeSpan GetDelay(int retryCount)
    {
        var delay = Strategy switch
        {
            BackoffStrategy.Exponential => BaseDelay * Math.Pow(2, retryCount),
            BackoffStrategy.Fixed => BaseDelay,
            BackoffStrategy.Linear => BaseDelay * (retryCount + 1),
            _ => BaseDelay,
        };

        return TimeSpan.FromMilliseconds(
            Math.Min(delay.TotalMilliseconds, MaxDelay.TotalMilliseconds));
    }
}
