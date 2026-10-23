using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(
    Guid WorkspaceId,
    Guid InvitationId)
    : ICommand<Result<Guid>>,
      IWriteRequest, IAuthenticatedRequest,
      IWorkspaceRequest,
      IRequirePermission,
      IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class ResendInvitationCommandHandler
    : IRequestHandler<ResendInvitationCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IOneTimeTokenService _tokenService;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActorLookupService _actorLookup;
    private readonly IGuestResourceInvitationFacts _resourceFacts;

    public ResendInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        ICurrentRequestContext requestContext,
        IOneTimeTokenService tokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector,
        IDateTimeProvider dateTimeProvider,
        IActorLookupService actorLookup,
        IGuestResourceInvitationFacts resourceFacts)
    {
        _actorLookup = actorLookup;
        _resourceFacts = resourceFacts;
        _workspaceContext = workspaceContext;
        _requestContext = requestContext;
        _tokenService = tokenService;
        _secretEncryptor = secretEncryptor;
        _integrationEventCollector = integrationEventCollector;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(
        ResendInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await _workspaceContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                x => x.Id == request.InvitationId
                    && x.WorkspaceId == request.WorkspaceId,
                cancellationToken);
        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        // A resend re-issues the token that grants the invited role. A legacy Admin invitation
        // (created before Admin stopped being invitable) must not be re-armed by a non-Owner actor.
        var actorRole = await WorkspaceActorRoleLookup.GetActiveActorRoleAsync(
            _workspaceContext, request.WorkspaceId, _requestContext.UserId, cancellationToken);
        WorkspaceMemberRules.EnsureActorCanAssignRole(actorRole, invitation.Role);

        var workspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var now = _dateTimeProvider.UtcNow;
        var issued = _tokenService.Generate(TokenPurpose.WorkspaceInvitation);
        invitation.Resend(
            InvitationTokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            now,
            TimeSpan.FromDays(7),
            _requestContext.UserId);

        // A resource invitation keeps its resource-invitation email when a workspace
        // Owner/Admin resends it: the event carries the resource and inviter names.
        string? resourceName = null;
        string? inviterName = null;
        var grant = invitation.ResourceGrant;
        if (grant is not null)
        {
            resourceName = await _resourceFacts.ResolveTitleAsync(
                grant.ResourceKind, grant.ResourceId, request.WorkspaceId, cancellationToken);
            if (resourceName is null)
                throw new NotFoundException(grant.ResourceKind, grant.ResourceId);
            inviterName = (await _actorLookup.FindAsync(invitation.InvitedBy, cancellationToken))?.Name;
        }

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
                ActorUserId: _requestContext.UserId,
                OccurredAt: now,
                ResourceKind: grant?.ResourceKind,
                ResourceId: grant?.ResourceId,
                ResourceName: resourceName,
                InviterName: inviterName));

        return Result<Guid>.Success(invitation.Id);
    }
}
