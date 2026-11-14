using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Permissions.Commands.SetChannelPermissions;

/// <summary>
/// Replaces the overrides of a channel's default permission set (UC-CHT-H01): for a role (manager, member, guest) and an action (post,
/// react, attach, pin, broadcast, invite), allow or deny. The set is the whole truth, so sending fewer entries removes the rest.
/// Needs a channel manager, a Workspace Owner or Admin; a stale expectedVersion is 412. Read-only is a deny of post for member and guest.
/// </summary>
public record SetChannelPermissionsCommand(
    Guid WorkspaceId, Guid ChannelId, IReadOnlyList<ChannelPermissionEntryDto> Entries, long ExpectedVersion)
    : ICommand<Result<ChannelPermissionsDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetChannelPermissionsCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatAuditFacts audit) : IRequestHandler<SetChannelPermissionsCommand, Result<ChannelPermissionsDto>>
{
    public async Task<Result<ChannelPermissionsDto>> Handle(SetChannelPermissionsCommand request, CancellationToken cancellationToken)
    {
        var requested = request.Entries
            .Select(e => new ChannelPermissionEntry(
                ChannelPermissionWire.SubjectFromWire(e.Subject)!.Value,
                ChannelPermissionWire.ActionFromWire(e.Action)!.Value,
                ChannelPermissionWire.EffectFromWire(e.Effect)!.Value))
            .ToList();

        var dto = await ChannelPermissionUpdates.ApplyAsync(
            db, members, audit, request.WorkspaceId, request.ChannelId, requestContext.UserId, requested, clock.UtcNow, cancellationToken);
        return Result<ChannelPermissionsDto>.Success(dto);
    }
}

public class SetChannelPermissionsCommandValidator : AbstractValidator<SetChannelPermissionsCommand>
{
    public SetChannelPermissionsCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.Entries).NotNull()
            .Must(entries => entries is not null && entries.Count <= ChatLimits.PermissionOverridesMax * 2)
            .WithMessage($"At most {ChatLimits.PermissionOverridesMax} overrides can be set.");
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Subject).Must(v => ChannelPermissionWire.SubjectFromWire(v) is not null)
                .WithMessage("subject must be manager, member or guest.");
            entry.RuleFor(e => e.Action).Must(v => ChannelPermissionWire.ActionFromWire(v) is not null)
                .WithMessage("action must be post, react, attach, pin, broadcast or invite.");
            entry.RuleFor(e => e.Effect).Must(v => ChannelPermissionWire.EffectFromWire(v) is not null)
                .WithMessage("effect must be allow or deny.");
        });
    }
}
