using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;

/// <summary>
/// The issuer revokes their own resource invitation (WSP-026). Authorization and
/// selection follow <c>ResendResourceInvitationCommand</c>: the pipeline requires
/// manage permission on the resource and the handler selects only an invitation
/// the actor issued for this exact resource. Revoking invalidates the token: a
/// revoked invitation can no longer be accepted.
/// </summary>
public sealed record RevokeResourceInvitationCommand(
    string ResourceKind,
    Guid ResourceId,
    Guid InvitationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest,
      IRequirePermission, IRequireVerifiedEmail
{
    internal ResourceKind Kind => global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(ResourceKind, out var kind)
        ? kind
        : throw new ArgumentException($"Invalid resource kind '{ResourceKind}'.", nameof(ResourceKind));

    PermissionAction IRequirePermission.Action => Kind.Value switch
    {
        WorkspaceInvitationResourceGrant.BoardKind => PermissionAction.ManageBoardPermission,
        WorkspaceInvitationResourceGrant.PageKind => PermissionAction.ManagePagePermission,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);
}

public sealed class RevokeResourceInvitationCommandHandler
    : IRequestHandler<RevokeResourceInvitationCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public RevokeResourceInvitationCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result> Handle(RevokeResourceInvitationCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var actorId = _requestContext.UserId;
        var kind = request.Kind.Value;

        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(
                i => i.Id == request.InvitationId
                     && i.WorkspaceId == workspaceId
                     && i.InvitedBy == actorId
                     && i.ResourceGrant != null
                     && i.ResourceGrant.ResourceKind == kind
                     && i.ResourceGrant.ResourceId == request.ResourceId,
                ct);
        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        var now = _dateTimeProvider.UtcNow;
        if (now >= invitation.ExpiresAt)
            invitation.Expire(now);
        else
            invitation.Revoke(actorId, now);

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "RevokeResourceInvitation",
            ResourceRef.Create(request.Kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct);

        return Result.Success();
    }
}
