using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Conversations.DTOs;
using Notrelix.Application.Features.Chat.Conversations.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Conversations.Commands.SetConversationVisibility;

/// <summary>
/// Hides a direct or group conversation from the caller's own list, or shows it again (UC-CHT-E07). The change is for this person
/// only: the other participants keep seeing it. A message in the conversation shows it again for everyone who hid it. The change
/// is idempotent: hiding a hidden conversation changes nothing.
/// </summary>
public record SetConversationVisibilityCommand(Guid WorkspaceId, Guid ConversationId, bool Hidden)
    : ICommand<Result<ConversationVisibilityDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetConversationVisibilityCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<SetConversationVisibilityCommand, Result<ConversationVisibilityDto>>
{
    public async Task<Result<ConversationVisibilityDto>> Handle(SetConversationVisibilityCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ConversationAccess.LoadParticipantAsync(db, request.WorkspaceId, request.ConversationId, userId, cancellationToken);
        var mine = space.GetActiveMembership(userId)!;

        if (request.Hidden) mine.Hide(clock.UtcNow);
        else mine.Restore();

        return Result<ConversationVisibilityDto>.Success(new ConversationVisibilityDto(space.Id, mine.IsHidden));
    }
}

public class SetConversationVisibilityCommandValidator : AbstractValidator<SetConversationVisibilityCommand>
{
    public SetConversationVisibilityCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
