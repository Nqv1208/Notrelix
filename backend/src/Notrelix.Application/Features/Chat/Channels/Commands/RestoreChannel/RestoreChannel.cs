using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Channels.Commands.RestoreChannel;

/// <summary>
/// Restores a deleted channel inside its 30-day window (UC-CHT-A08, CHT-018). Who may do it is exactly who may delete it: the same
/// visibility and manager rules, so a private channel's existence is not revealed to anyone who could not have deleted it. The
/// channel comes back as it was, with its members, roles and settings untouched. A name that was taken meanwhile refuses the
/// restore with the name-conflict code; past the window the restore is refused with the restore-window-elapsed code.
/// </summary>
public record RestoreChannelCommand(Guid WorkspaceId, Guid ChannelId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RestoreChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    IAccessChangeSignal access,
    ChatAuditFacts audit) : IRequestHandler<RestoreChannelCommand, Result>
{
    public async Task<Result> Handle(RestoreChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var now = clock.UtcNow;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadDeletedChannelAsync(db, requestContext.RequireAccountId(), request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        if (space.IsPastRestoreWindow(now))
            throw new ConflictException("The 30-day window to restore this channel has passed.", ChatRuleCodes.RestoreWindowElapsed);

        // An archived channel gives its name up for other channels (it may be restored archived), so only a live name can clash.
        if (space.ArchivedAt is null)
            await ChannelAccess.RequireUniqueNameAsync(db, request.WorkspaceId, space.Name, space.Id, cancellationToken);

        space.Restore(userId, now);
        await audit.ChannelRestoredAsync(space, cancellationToken);
        access.Channel(request.WorkspaceId, request.ChannelId);
        return Result.Success();
    }
}

public class RestoreChannelCommandValidator : AbstractValidator<RestoreChannelCommand>
{
    public RestoreChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
