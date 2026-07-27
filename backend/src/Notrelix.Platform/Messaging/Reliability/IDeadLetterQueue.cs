namespace Notrelix.Platform.Messaging.Reliability;

public sealed record DeadLetterEntry
{
    public Guid Id { get; init; }
    public required string EventName { get; init; }
    public int EventVersion { get; init; }
    public required byte[] Payload { get; init; }
    public required string Reason { get; init; }
    public int RetryCount { get; init; }
    public int PoisonCount { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset DeadLetteredAt { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? WorkspaceId { get; init; }
}

public interface IDeadLetterQueue
{
    Task DeadLetterAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetterEntry>> GetDeadLettersAsync(
        string? eventName = null,
        CancellationToken cancellationToken = default);
    Task<int> GetDeadLetterCountAsync(CancellationToken cancellationToken = default);
    Task ReplayAsync(Guid entryId, CancellationToken cancellationToken = default);
}
