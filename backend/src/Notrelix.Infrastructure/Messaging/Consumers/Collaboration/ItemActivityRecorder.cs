using System.Text.Json;
using Notrelix.Application.Features.Automation.Public.ActivityAttribution;
using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Projections.Activity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>
/// Appends one Item history row for a Work Management fact. The row is keyed by the source event and its
/// type, so a redelivery of the same event (a retry after the row was saved but before the acknowledgement)
/// adds nothing. Detail values are short words, never a document body or a secret.
/// </summary>
public sealed class ItemActivityRecorder
{
    private const int MaxTitleLength = 300;
    private const string Work = "work";

    private readonly IActivityProjectionDbContext _context;
    private readonly ILogger<ItemActivityRecorder> _logger;
    private readonly IAutomationActivityFacts _automation;

    public ItemActivityRecorder(IActivityProjectionDbContext context, ILogger<ItemActivityRecorder> logger, IAutomationActivityFacts automation)
    {
        _automation = automation;
        _context = context;
        _logger = logger;
    }

    public async Task RecordAsync(
        Guid workspaceId,
        Guid itemId,
        Guid? boardId,
        string activityType,
        Guid sourceEventId,
        Guid? actorUserId,
        DateTimeOffset occurredAt,
        string title,
        string? itemTitle,
        IReadOnlyDictionary<string, string?>? data,
        CancellationToken cancellationToken,
        Guid? automationExecutionId = null)
    {
        var alreadyRecorded = await _context.WorkspaceActivityLogs.AnyAsync(
            r => r.SourceEventId == sourceEventId && r.ActivityType == activityType, cancellationToken);
        if (alreadyRecorded)
        {
            _logger.LogDebug("Item activity {ActivityType} for event {EventId} already recorded", activityType, sourceEventId);
            return;
        }

        // A change an Automation rule made carries the execution as its causation: the history says which rule.
        if (automationExecutionId is { } execution
            && await _automation.FindRuleNameAsync(workspaceId, execution, cancellationToken) is { } ruleName)
        {
            data = new Dictionary<string, string?>(data ?? new Dictionary<string, string?>()) { ["rule"] = ruleName };
        }

        var detail = data is null
            ? null
            : JsonSerializer.SerializeToDocument(data.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value));

        _context.WorkspaceActivityLogs.Add(WorkspaceActivityLogRecord.Create(
            workspaceId: workspaceId,
            sourceContext: Work,
            activityType: activityType,
            subjectType: ItemActivityTypes.SubjectType,
            subjectId: itemId,
            occurredAt: occurredAt,
            sourceEventId: sourceEventId,
            actorUserId: actorUserId,
            subjectDisplayName: itemTitle,
            targetType: boardId is null ? null : "Board",
            targetId: boardId,
            resourceType: ItemActivityTypes.ResourceKind,
            resourceId: itemId,
            resourceDisplayName: itemTitle,
            title: title.Length <= MaxTitleLength ? title : title[..(MaxTitleLength - 1)] + "…",
            dataJson: detail));
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Item activity projected: {ActivityType} {ItemId}", activityType, itemId);
    }
}
