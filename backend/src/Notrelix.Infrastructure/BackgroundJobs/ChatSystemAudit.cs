using System.Text.Json;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Writes the Chat facts that have no person behind them to the Governance audit trail as system facts (UC-CHT-H05, H06), the way the purge
/// jobs write theirs: an <c>AuditLog</c> row with actor type System, added to the unit of work of the batch or export it describes, so the fact
/// and the work are stored together or not at all. The metadata holds counts, the rule and the ids; never message content.
/// </summary>
internal sealed class ChatSystemAudit(ApplicationDbContext context, IDateTimeProvider clock) : IChatSystemAudit
{
    public const string RetentionApplied = "ChatRetentionApplied";
    public const string ExportCompleted = "ChatExportCompleted";

    public Task RecordRetentionBatchAsync(RetentionBatchFact fact, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.SerializeToDocument(new
        {
            mode = fact.Mode,
            retentionDays = fact.RetentionDays,
            cutoff = fact.Cutoff,
            messages = fact.Messages,
            threads = fact.Threads,
        });
        context.EnterpriseAuditLogs.Add(new AuditLog(
            fact.WorkspaceId, null, "System", RetentionApplied, ChatAuditFacts.ChannelKind, fact.SpaceId, null, null,
            "Warning", "Succeeded", null, null, null, null, null, null, null, metadata, clock.UtcNow));
        return Task.CompletedTask;
    }

    public Task RecordExportCompletedAsync(ExportCompletedFact fact, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.SerializeToDocument(new
        {
            requestedBy = fact.RequestedBy,
            outcome = fact.Outcome,
            messages = fact.Messages,
            spaces = fact.Spaces,
            excludedSpaces = fact.ExcludedSpaces,
            tombstonesOmitted = fact.Tombstones,
            sizeBytes = fact.SizeBytes,
        });
        context.EnterpriseAuditLogs.Add(new AuditLog(
            fact.WorkspaceId, null, "System", ExportCompleted, ChatAuditFacts.ExportKind, fact.JobId, "User", fact.SubjectUserId,
            "Info", fact.Outcome == "ready" ? "Succeeded" : "Failed", null, null, null, null, null, null, null, metadata, clock.UtcNow));
        return Task.CompletedTask;
    }
}
