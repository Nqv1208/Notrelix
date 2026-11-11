using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Channels.Commands.ArchiveChannel;

/// <summary>
/// Archives or unarchives a channel (UC-CHT-A08). An archived channel is read-only and stays searchable by its
/// members. Unarchiving checks the name again, since another channel may have taken it meanwhile.
/// </summary>
public record ArchiveChannelCommand(Guid WorkspaceId, Guid ChannelId, bool Archived, long ExpectedVersion)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ArchiveChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages,
    IAccessChangeSignal access) : IRequestHandler<ArchiveChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(ArchiveChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        if (!request.Archived)
            await ChannelAccess.RequireUniqueNameAsync(db, request.WorkspaceId, space.Name, space.Id, cancellationToken);

        if (space.SetArchived(request.Archived, userId, clock.UtcNow))
        {
            var content = request.Archived ? ChatSystemMessages.Archived : ChatSystemMessages.Unarchived;
            await systemMessages.AnnounceAsync(space, userId, content, cancellationToken);
            // An archived channel is read-only for everyone: its live subscriptions close (UC-CHT-C06).
            if (request.Archived) access.Channel(request.WorkspaceId, request.ChannelId);
        }
        return Result<ChannelDto>.Success(ChannelDto.From(space, space.GetActiveMembership(userId)));
    }
}

public class ArchiveChannelCommandValidator : AbstractValidator<ArchiveChannelCommand>
{
    public ArchiveChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
