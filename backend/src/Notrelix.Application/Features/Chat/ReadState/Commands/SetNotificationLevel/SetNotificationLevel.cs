using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Time;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.ReadState.Commands.SetNotificationLevel;

/// <summary>
/// Sets how the caller is notified in a space (UC-CHT-D05, D06). A mute is the level None with an optional until time: a
/// past until changes nothing, and a level other than None clears the until. Settings are per person and space, and the
/// last write wins: the membership row has no version. A space the caller is not an active member of is NotFound.
/// </summary>
public record SetNotificationLevelCommand(Guid WorkspaceId, Guid SpaceId, string? Level, DateTimeOffset? MutedUntil)
    : ICommand<Result<NotificationSettingsDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetNotificationLevelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<SetNotificationLevelCommand, Result<NotificationSettingsDto>>
{
    public async Task<Result<NotificationSettingsDto>> Handle(SetNotificationLevelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var membership = space.GetActiveMembership(userId)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var now = clock.UtcNow;
        membership.SetNotificationLevel(SpaceWire.NotificationLevelFromWire(request.Level!), request.MutedUntil, now);

        var level = membership.EffectiveNotificationLevel(now);
        var muted = level == NotificationLevel.None;
        return Result<NotificationSettingsDto>.Success(new NotificationSettingsDto(
            space.Id,
            level.ToWire(),
            muted,
            muted ? membership.MutedUntil : null));
    }
}

public class SetNotificationLevelCommandValidator : AbstractValidator<SetNotificationLevelCommand>
{
    public SetNotificationLevelCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Level)
            .Must(level => level is "all" or "mentions" or "none")
            .WithMessage("level must be all, mentions or none.");
        RuleFor(x => x)
            .Must(x => x.MutedUntil is null || x.Level == "none")
            .WithMessage("An until time applies only to a mute, which is the level none.");
    }
}
