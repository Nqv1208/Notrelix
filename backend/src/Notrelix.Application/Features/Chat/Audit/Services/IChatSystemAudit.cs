namespace Notrelix.Application.Features.Chat.Audit.Services;

/// <summary>One batch the retention job removed from one space, for the Audit fact (counts only, never content; UC-CHT-H05).</summary>
public sealed record RetentionBatchFact(
    Guid AccountId, Guid WorkspaceId, Guid SpaceId, string Mode, int RetentionDays, DateTimeOffset Cutoff, int Messages, int Threads);

/// <summary>An export the worker finished, for the Audit fact (counts only, never content; UC-CHT-H06).</summary>
public sealed record ExportCompletedFact(
    Guid AccountId, Guid WorkspaceId, Guid JobId, Guid RequestedBy, Guid SubjectUserId, string Outcome,
    int Messages, int Spaces, int ExcludedSpaces, int Tombstones, long SizeBytes);

/// <summary>
/// Chat's Audit facts that have no person behind them (UC-CHT-H05, H06): a retention batch and a finished export. A job has no request
/// principal, so Infrastructure writes these to the Governance audit trail as system facts, in the transaction of the work they describe,
/// the way the purge jobs do. The facts that a person causes go through <see cref="ChatAuditFacts"/> and the shared audit contract.
/// </summary>
public interface IChatSystemAudit
{
    Task RecordRetentionBatchAsync(RetentionBatchFact fact, CancellationToken cancellationToken);

    Task RecordExportCompletedAsync(ExportCompletedFact fact, CancellationToken cancellationToken);
}
