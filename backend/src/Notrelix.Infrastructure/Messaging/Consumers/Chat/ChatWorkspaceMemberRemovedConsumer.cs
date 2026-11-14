using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Chat.Members.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <summary>
/// Ends the channel memberships of a person who left or was removed from a Workspace (workspace.member.removed). The
/// Application service does the work; this adapter takes the scope and the time from the fact. A fact without a Workspace
/// id has nothing to end and is ignored.
/// </summary>
[OrderingNotApplicable("Ending a person's channel memberships in one Workspace is an idempotent per-message convergence: a membership already ended is a no-op, and the effect is a fact about the person's standing that the removal fact carries.")]
public sealed class ChatWorkspaceMemberRemovedConsumer : IConsumer<WorkspaceMemberRemovedIntegrationEvent>
{
    private readonly ChatWorkspaceExit _exit;

    public ChatWorkspaceMemberRemovedConsumer(ChatWorkspaceExit exit) => _exit = exit;

    public async Task Consume(ConsumeContext<WorkspaceMemberRemovedIntegrationEvent> context)
    {
        var message = context.Message;
        if (message.WorkspaceId is not { } workspaceId || workspaceId == Guid.Empty)
            return;

        var leftAt = message.OccurredAt == default ? DateTimeOffset.UtcNow : message.OccurredAt;
        await _exit.EndMembershipsAsync(workspaceId, message.UserId, leftAt, context.CancellationToken);
    }
}
