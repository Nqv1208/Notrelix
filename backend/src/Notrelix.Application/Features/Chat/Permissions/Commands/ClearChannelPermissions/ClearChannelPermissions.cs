using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Permissions;

namespace Notrelix.Application.Features.Chat.Permissions.Commands.ClearChannelPermissions;

/// <summary>
/// Removes every override of a channel, so each role is back on its default (UC-CHT-H01). Idempotent: a channel with no overrides
/// changes nothing and writes no fact. Needs a channel manager, a Workspace Owner or Admin; a stale expectedVersion is 412.
/// </summary>
public record ClearChannelPermissionsCommand(Guid WorkspaceId, Guid ChannelId, long ExpectedVersion)
    : ICommand<Result<ChannelPermissionsDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ClearChannelPermissionsCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatAuditFacts audit) : IRequestHandler<ClearChannelPermissionsCommand, Result<ChannelPermissionsDto>>
{
    public async Task<Result<ChannelPermissionsDto>> Handle(ClearChannelPermissionsCommand request, CancellationToken cancellationToken)
    {
        var dto = await ChannelPermissionUpdates.ApplyAsync(
            db, members, audit, request.WorkspaceId, request.ChannelId, requestContext.UserId, Array.Empty<ChannelPermissionEntry>(), clock.UtcNow, cancellationToken);
        return Result<ChannelPermissionsDto>.Success(dto);
    }
}

public class ClearChannelPermissionsCommandValidator : AbstractValidator<ClearChannelPermissionsCommand>
{
    public ClearChannelPermissionsCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
