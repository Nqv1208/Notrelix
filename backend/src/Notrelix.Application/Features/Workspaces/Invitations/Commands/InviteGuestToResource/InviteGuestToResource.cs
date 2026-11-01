using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;

/// <summary>
/// Resource invitation by email (WSP-026): a principal who may manage the
/// access of one Board or Page invites an outsider. Accepting it creates (or
/// reuses) a Guest Workspace membership plus the explicit entry on that one
/// resource, capped at Editor (GOV-030). It never creates a Member, never adds
/// Space or Team entries and never stores an <c>ExternalEmail</c> subject.
/// </summary>
/// <remarks>
/// Authorized as <c>ManageBoardPermission</c> / <c>ManagePagePermission</c> on the
/// resource, and the requested level is also checked against the inviter's own
/// authority (a principal cannot invite above their level).
/// </remarks>
public record InviteGuestToResourceCommand(
    string ResourceKind,
    Guid ResourceId,
    string Email,
    string Level
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest,
    IRequirePermission, IRequireGrantPermission, IRequireVerifiedEmail
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

    // Governance rank vocabulary: Viewer=1, Commenter=2, Editor=3. A level above
    // Editor ranks higher and is rejected by the Domain rule (GOV-030).
    int IRequireGrantPermission.RequestedPermissionRank => Level?.Trim().ToLowerInvariant() switch
    {
        "viewer" => 1,
        "commenter" => 2,
        "editor" => 3,
        "manager" => 4,
        "owner" => 5,
        _ => 0
    };
}

public class InviteGuestToResourceCommandHandler : IRequestHandler<InviteGuestToResourceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IAuditService _auditService;
    private readonly IActorLookupService _actorLookup;
    private readonly IGuestResourceInvitationFacts _resourceFacts;

    public InviteGuestToResourceCommandHandler(
        IWorkspaceDbContext workspaceContext,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService oneTimeTokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector,
        IAuditService auditService,
        IActorLookupService actorLookup,
        IGuestResourceInvitationFacts resourceFacts)
    {
        _actorLookup = actorLookup;
        _resourceFacts = resourceFacts;
        _workspaceContext = workspaceContext;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _oneTimeTokenService = oneTimeTokenService;
        _secretEncryptor = secretEncryptor;
        _integrationEventCollector = integrationEventCollector;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(InviteGuestToResourceCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var actorId = _requestContext.UserId;

        var workspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), workspaceId);

        // GOV-030 + WSP-026 invariants live in the Domain value object: only a
        // Board or Page, level Viewer/Commenter/Editor.
        var grant = WorkspaceInvitationResourceGrant.ForNewInvitation(request.Kind.Value, request.ResourceId, request.Level);

        var title = await _resourceFacts.ResolveTitleAsync(grant.ResourceKind, grant.ResourceId, workspaceId, ct);
        if (title is null)
            throw new NotFoundException(request.ResourceKind, request.ResourceId);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var hasActiveInvitation = await _workspaceContext.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == workspaceId
                           && i.Email == cleanEmail
                           && i.Status == WorkspaceInvitationStatus.Pending
                           && i.ExpiresAt > now
                           && i.ResourceGrant != null
                           && i.ResourceGrant.ResourceKind == grant.ResourceKind
                           && i.ResourceGrant.ResourceId == grant.ResourceId, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("A pending invitation already exists for this email address on this resource.");

        var issuedToken = _oneTimeTokenService.Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = WorkspaceInvitation.CreateForResource(
            workspace.AccountId,
            workspaceId,
            cleanEmail,
            grant,
            InvitationTokenHash.Create(issuedToken.TokenHash),
            issuedToken.HashVersion,
            actorId,
            now);

        _workspaceContext.WorkspaceInvitations.Add(invitation);

        var protectedToken = _secretEncryptor.Protect(
            issuedToken.RawToken,
            OneTimeTokenProtectionPurposes.WorkspaceInvitation);

        _integrationEventCollector.Add(
            new WorkspaceInvitationDeliveryRequestedIntegrationEventV1(
                EventId: Guid.CreateVersion7(),
                InvitationId: invitation.Id,
                AccountId: workspace.AccountId,
                WorkspaceId: workspace.Id,
                RecipientEmail: cleanEmail,
                ProtectedToken: protectedToken,
                HashVersion: issuedToken.HashVersion,
                TokenGeneration: invitation.TokenGeneration,
                ExpiresAt: invitation.ExpiresAt,
                InvitedBy: actorId,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: actorId,
                OccurredAt: now,
                ResourceKind: grant.ResourceKind,
                ResourceId: grant.ResourceId,
                ResourceName: title,
                InviterName: (await _actorLookup.FindAsync(actorId, ct))?.Name));

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "InviteGuestToResource",
            ResourceRef.Create(request.Kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
