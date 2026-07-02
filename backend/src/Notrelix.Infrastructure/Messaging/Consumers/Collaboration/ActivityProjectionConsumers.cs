using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Projections.Activity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class BoardCreatedActivityConsumer : IConsumer<BoardCreatedIntegrationEvent>
{
    private readonly IActivityProjectionDbContext _context;
    private readonly ILogger<BoardCreatedActivityConsumer> _logger;

    public BoardCreatedActivityConsumer(IActivityProjectionDbContext context, ILogger<BoardCreatedActivityConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BoardCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null) return;

        var record = WorkspaceActivityLogRecord.Create(
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "work",
            activityType: "work.board-created",
            subjectType: "Board",
            subjectId: msg.BoardId,
            occurredAt: msg.OccurredAt,
            sourceEventId: msg.EventId,
            actorUserId: msg.ActorUserId,
            subjectDisplayName: msg.Name,
            title: $"Board \"{msg.Name}\" was created");

        _context.WorkspaceActivityLogs.Add(record);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug("Activity projected: board-created {BoardId}", msg.BoardId);
    }
}

public sealed class CommentCreatedActivityConsumer : IConsumer<CommentCreatedIntegrationEvent>
{
    private readonly IActivityProjectionDbContext _context;
    private readonly ILogger<CommentCreatedActivityConsumer> _logger;

    public CommentCreatedActivityConsumer(IActivityProjectionDbContext context, ILogger<CommentCreatedActivityConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CommentCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null) return;

        var record = WorkspaceActivityLogRecord.Create(
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "collaboration",
            activityType: "collab.comment-created",
            subjectType: "Comment",
            subjectId: msg.CommentId,
            occurredAt: msg.OccurredAt,
            sourceEventId: msg.EventId,
            actorUserId: msg.AuthorId,
            targetType: msg.TargetType,
            targetId: msg.TargetId,
            title: $"Comment added to {msg.TargetType.ToLowerInvariant()}");

        _context.WorkspaceActivityLogs.Add(record);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug("Activity projected: comment-created {CommentId}", msg.CommentId);
    }
}

public sealed class MentionCreatedActivityConsumer : IConsumer<MentionCreatedIntegrationEvent>
{
    private readonly IActivityProjectionDbContext _context;
    private readonly ILogger<MentionCreatedActivityConsumer> _logger;

    public MentionCreatedActivityConsumer(IActivityProjectionDbContext context, ILogger<MentionCreatedActivityConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MentionCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null) return;

        var record = WorkspaceActivityLogRecord.Create(
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "collaboration",
            activityType: "collab.user-mentioned",
            subjectType: "Mention",
            subjectId: msg.MentionId,
            occurredAt: msg.OccurredAt,
            sourceEventId: msg.EventId,
            actorUserId: msg.MentionedByUserId,
            targetType: msg.TargetType,
            targetId: msg.TargetId,
            title: $"User mentioned in {msg.TargetType.ToLowerInvariant()}");

        _context.WorkspaceActivityLogs.Add(record);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug("Activity projected: user-mentioned {MentionId}", msg.MentionId);
    }
}

public sealed class WorkspaceMemberAddedActivityConsumer : IConsumer<WorkspaceMemberAddedIntegrationEvent>
{
    private readonly IActivityProjectionDbContext _context;
    private readonly ILogger<WorkspaceMemberAddedActivityConsumer> _logger;

    public WorkspaceMemberAddedActivityConsumer(IActivityProjectionDbContext context, ILogger<WorkspaceMemberAddedActivityConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WorkspaceMemberAddedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null) return;

        var record = WorkspaceActivityLogRecord.Create(
            workspaceId: msg.WorkspaceId.Value,
            sourceContext: "workspace",
            activityType: "workspace.member-invited",
            subjectType: "WorkspaceMember",
            subjectId: msg.UserId,
            occurredAt: msg.OccurredAt,
            sourceEventId: msg.EventId,
            actorUserId: msg.ActorUserId,
            subjectDisplayName: msg.Role,
            title: $"Member added with role \"{msg.Role}\"");

        _context.WorkspaceActivityLogs.Add(record);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug("Activity projected: member-invited {UserId}", msg.UserId);
    }
}
