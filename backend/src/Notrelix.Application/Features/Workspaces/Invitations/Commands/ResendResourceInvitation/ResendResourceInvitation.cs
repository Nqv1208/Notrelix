using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;

/// <summary>
/// The issuer resends their own pending or expired resource invitation (WSP-026).
/// The pipeline requires <c>ManageBoardPermission</c> / <c>ManagePagePermission</c>
/// on the resource, so an issuer who has lost that permission is denied before the
/// handler runs. The handler selects only an invitation the actor issued for this
/// exact resource: anyone else's invitation is not found. Workspace Owner/Admin
/// resend any invitation through the workspace endpoint.
/// </summary>
public sealed record ResendResourceInvitationCommand(
    string ResourceKind,
    Guid ResourceId,
    Guid InvitationId)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest,
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

public sealed class ResendResourceInvitationCommandHandler
    : IRequestHandler<ResendResourceInvitationCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IActorLookupService _actorLookup;
    private readonly IGuestResourceInvitationFacts _resourceFacts;
    private readonly IOneTimeTokenService _tokenService;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public ResendResourceInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        ICurrentRequestContext requestContext,
        IActorLookupService actorLookup,
        IGuestResourceInvitationFacts resourceFacts,
        IOneTimeTokenService tokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _workspaceContext = workspaceContext;
        _requestContext = requestContext;
        _actorLookup = actorLookup;
        _resourceFacts = resourceFacts;
        _tokenService = tokenService;
        _secretEncryptor = secretEncryptor;
        _integrationEventCollector = integrationEventCollector;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(ResendResourceInvitationCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var actorId = _requestContext.UserId;
        var kind = request.Kind.Value;

        var invitation = await _workspaceContext.WorkspaceInvitations
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

        var workspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);
        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), workspaceId);

        var title = await _resourceFacts.ResolveTitleAsync(kind, request.ResourceId, workspaceId, ct);
        if (title is null)
            throw new NotFoundException(request.ResourceKind, request.ResourceId);

        var now = _dateTimeProvider.UtcNow;
        var issued = _tokenService.Generate(TokenPurpose.WorkspaceInvitation);
        invitation.Resend(
            InvitationTokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            now,
            TimeSpan.FromDays(7),
            actorId);

        var inviter = await _actorLookup.FindAsync(actorId, ct);
        var eventId = Guid.CreateVersion7();
        _integrationEventCollector.Add(
            new WorkspaceInvitationDeliveryRequestedIntegrationEventV1(
                EventId: eventId,
                InvitationId: invitation.Id,
                AccountId: workspace.AccountId,
                WorkspaceId: workspace.Id,
                RecipientEmail: invitation.Email,
                ProtectedToken: _secretEncryptor.Protect(
                    issued.RawToken,
                    OneTimeTokenProtectionPurposes.WorkspaceInvitation),
                HashVersion: issued.HashVersion,
                TokenGeneration: invitation.TokenGeneration,
                ExpiresAt: invitation.ExpiresAt,
                InvitedBy: invitation.InvitedBy,
                CorrelationId: eventId,
                ActorUserId: actorId,
                OccurredAt: now,
                ResourceKind: kind,
                ResourceId: request.ResourceId,
                ResourceName: title,
                InviterName: inviter?.Name));

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "ResendResourceInvitation",
            ResourceRef.Create(request.Kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
