using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

/// <summary>
/// Workspace invitation (WSP-026): role Member or Guest (Admin stays accepted for
/// existing callers), optional initial Team/Space entries. Naming entries is
/// authorized by the pipeline as ManageTeams (Team entries) or ManageSpaces
/// (Space-only) on the Workspace, because Team composition and Space members
/// are managed by Workspace Owner/Admin (GOV-028); there is no handler role check.
/// </summary>
public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    IReadOnlyList<InvitationInitialEntryInput>? InitialEntries = null
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    PermissionAction IRequirePermission.Action =>
        InitialEntries is not { Count: > 0 }
            ? PermissionAction.InviteMember
            : InitialEntries.Any(e => string.Equals(e.Kind, nameof(WorkspaceInvitationEntryKind.Team), StringComparison.OrdinalIgnoreCase))
                ? PermissionAction.ManageTeams
                : PermissionAction.ManageSpaces;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly InvitationInitialEntries _initialEntries;

    public InviteMemberCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IActorLookupService actorLookup,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService oneTimeTokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector,
        InvitationInitialEntries initialEntries)
    {
        _initialEntries = initialEntries;
        _workspaceContext = workspaceContext;
        _actorLookup = actorLookup;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _oneTimeTokenService = oneTimeTokenService;
        _secretEncryptor = secretEncryptor;
        _integrationEventCollector = integrationEventCollector;
    }

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var workspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var entries = ToDomainEntries(request.InitialEntries);
        if (entries.Count > 0)
        {
            if (!await _initialEntries.AllTargetsEligibleAsync(request.WorkspaceId, entries, ct))
                return Result<Guid>.Failure("Every initial team or space must be an active team or space of this workspace.");
        }

        var hasActiveInvitation = await _workspaceContext.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == request.WorkspaceId
                           && i.Email == cleanEmail
                           && i.Status == WorkspaceInvitationStatus.Pending
                           && i.ResourceGrant == null
                           && i.ExpiresAt > now, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("A pending invitation already exists for this email address.");

        var issuedToken = _oneTimeTokenService.Generate(TokenPurpose.WorkspaceInvitation);
        var invitationTokenHash = InvitationTokenHash.Create(issuedToken.TokenHash);

        var invitation = WorkspaceInvitation.Create(
            workspace.AccountId,
            request.WorkspaceId,
            cleanEmail,
            request.Role,
            invitationTokenHash,
            issuedToken.HashVersion,
            _requestContext.UserId,
            now,
            initialEntries: entries);

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
                InvitedBy: _requestContext.UserId,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: _requestContext.UserId,
                OccurredAt: now));

        return Result<Guid>.Success(invitation.Id);
    }

    private static IReadOnlyList<WorkspaceInvitationInitialEntry> ToDomainEntries(
        IReadOnlyList<InvitationInitialEntryInput>? inputs)
    {
        if (inputs is null || inputs.Count == 0) return [];

        return inputs.Select(i => WorkspaceInvitationInitialEntry.Parse(i.Kind, i.TargetId, i.Role)).ToArray();
    }
}
