using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Members.Commands.AddChannelMembers;

/// <summary>
/// Adds Workspace members to a channel (UC-CHT-A06). In a private channel any member may invite; in a public
/// channel only a manager, a Workspace Owner or Admin may add people. A person who is not in the Workspace is
/// skipped and reported. A Guest can be added only explicitly, which this command is.
/// </summary>
public record AddChannelMembersCommand(Guid WorkspaceId, Guid ChannelId, IReadOnlyList<Guid> UserIds)
    : ICommand<Result<AddChannelMembersResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMemberInvite;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddChannelMembersCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages) : IRequestHandler<AddChannelMembersCommand, Result<AddChannelMembersResultDto>>
{
    public async Task<Result<AddChannelMembersResultDto>> Handle(AddChannelMembersCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (space.IsPublic)
            ChannelAccess.RequireManager(space, caller, userId);

        var added = new List<Guid>();
        var skipped = new List<Guid>();
        var now = clock.UtcNow;
        foreach (var candidate in request.UserIds.Distinct())
        {
            var fact = await members.FindAsync(request.WorkspaceId, candidate, cancellationToken);
            if (fact is not { IsActive: true })
            {
                skipped.Add(candidate);
                continue;
            }

            if (space.AddMember(candidate, MembershipRole.Member, now))
            {
                added.Add(candidate);
                await systemMessages.AnnounceAsync(space, userId, ChatSystemMessages.MemberAdded, cancellationToken);
            }
        }

        return Result<AddChannelMembersResultDto>.Success(new AddChannelMembersResultDto(added, skipped));
    }
}

public class AddChannelMembersCommandValidator : AbstractValidator<AddChannelMembersCommand>
{
    public AddChannelMembersCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserIds).NotEmpty().Must(ids => ids.Count <= 100)
            .WithMessage("At most 100 people can be added at once.");
        RuleForEach(x => x.UserIds).NotEmpty();
    }
}
