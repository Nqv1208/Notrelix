namespace Notrelix.Infrastructure.Messaging.Options;

/// <summary>
/// Batch bounds of the outbox dispatcher for ordered streams (<c>stream_key</c> set).
/// Bound from <see cref="SectionName"/> and validated on startup.
/// </summary>
public sealed class OutboxDispatchOptions
{
    public const string SectionName = "Messaging:Outbox";

    public const int MaxStreamBatchSize = 1_000;
    public const int MaxStreamMessagesPerPollLimit = 10_000;

    /// <summary>
    /// Most consecutive messages of ONE stream a poll claims and dispatches, in stream version order.
    /// Bounds how long a hot stream holds its lease and how many messages one crash can redeliver.
    /// </summary>
    public int StreamBatchSize { get; init; } = 100;

    /// <summary>
    /// Most ordered-stream messages one poll claims across ALL streams. Streams are served oldest head first and
    /// each contributes at most <see cref="StreamBatchSize"/>, so one hot stream cannot starve the others.
    /// </summary>
    public int MaxStreamMessagesPerPoll { get; init; } = 500;
}

public sealed class OutboxDispatchOptionsValidator : IValidateOptions<OutboxDispatchOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxDispatchOptions options)
    {
        if (options.StreamBatchSize is < 1 or > OutboxDispatchOptions.MaxStreamBatchSize)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:StreamBatchSize must be between 1 and {OutboxDispatchOptions.MaxStreamBatchSize}.");
        }

        if (options.MaxStreamMessagesPerPoll < options.StreamBatchSize
            || options.MaxStreamMessagesPerPoll > OutboxDispatchOptions.MaxStreamMessagesPerPollLimit)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:MaxStreamMessagesPerPoll must be between StreamBatchSize " +
                $"({options.StreamBatchSize}) and {OutboxDispatchOptions.MaxStreamMessagesPerPollLimit}.");
        }

        return ValidateOptionsResult.Success;
    }
}
