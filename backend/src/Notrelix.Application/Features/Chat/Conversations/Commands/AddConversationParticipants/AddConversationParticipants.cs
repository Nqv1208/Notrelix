using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Conversations.DTOs;
using Notrelix.Application.Features.Chat.Conversations.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Chat.Conversations.Commands.AddConversationParticipants;

/// <summary>
/// Adds people to a group conversation (UC-CHT-E03). Only a participant may add, and only to a group: a direct conversation takes no
/// one else (400 kind-not-supported). The group stays within its cap (422 limit-reached). A person who is not an active member of
/// the Workspace, or who a Guest caller cannot reach, is reported as skipped. A new participant reads from the point they join: the
/// history before that is not part of their view (the join sequence). Each addition announces itself with a system message.
/// </summary>
public record AddConversationParticipantsCommand(Guid WorkspaceId, Guid ConversationId, IReadOnlyList<Guid> UserIds)
    : ICommand<Result<AddConversationParticipantsResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMemberInvite;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddConversationParticipantsCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages) : IRequestHandler<AddConversationParticipantsCommand, Result<AddConversationParticipantsResultDto>>
{
    public async Task<Result<AddConversationParticipantsResultDto>> Handle(AddConversationParticipantsCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ConversationAccess.LoadParticipantAsync(db, request.WorkspaceId, request.ConversationId, userId, cancellationToken);
        if (space.Kind != SpaceKind.Group)
            throw new DomainBusinessRuleException(Notrelix.Domain.Chat.Rules.ChatRuleCodes.KindNotSupported, "Only a group conversation takes new participants.");

        var added = new List<Guid>();
        var skipped = new List<Guid>();
        var now = clock.UtcNow;
        foreach (var candidate in request.UserIds.Distinct())
        {
            var fact = await members.FindAsync(request.WorkspaceId, candidate, cancellationToken);
            var reachable = fact is { IsActive: true }
                && ConversationAccess.MayReach(caller, await ConversationAccess.SharesSpaceAsync(db, request.WorkspaceId, userId, candidate, cancellationToken));
            if (!reachable)
            {
                skipped.Add(candidate);
                continue;
            }

            if (space.AddParticipant(candidate, now))
            {
                added.Add(candidate);
                await systemMessages.AnnounceAsync(space, userId, ChatSystemMessages.MemberAdded, cancellationToken);
            }
        }

        return Result<AddConversationParticipantsResultDto>.Success(new AddConversationParticipantsResultDto(added, skipped));
    }
}

public class AddConversationParticipantsCommandValidator : AbstractValidator<AddConversationParticipantsCommand>
{
    public AddConversationParticipantsCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.UserIds).NotEmpty().Must(ids => ids.Count <= 100)
            .WithMessage("At most 100 people can be added at once.");
        RuleForEach(x => x.UserIds).NotEmpty();
    }
}
