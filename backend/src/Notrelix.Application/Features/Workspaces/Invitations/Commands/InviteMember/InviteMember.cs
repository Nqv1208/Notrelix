using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role
) : ICommand<Result<Guid>>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    PermissionAction IRequirePermission.Action => PermissionAction.InviteMember;
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

    public InviteMemberCommandHandler(
        IWorkspaceDbContext workspaceContext,
        IActorLookupService actorLookup,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService oneTimeTokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector)
    {
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
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var hasActiveInvitation = await _workspaceContext.WorkspaceInvitations
            .AnyAsync(i => i.WorkspaceId == request.WorkspaceId
                           && i.Email == cleanEmail
                           && i.Status == WorkspaceInvitationStatus.Pending
                           && i.ExpiresAt > now, ct);

        if (hasActiveInvitation)
            return Result<Guid>.Failure("Đã có một lời mời đang chờ xử lý dành cho email này.");

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
                InvitedBy: _requestContext.UserId,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: _requestContext.UserId,
                OccurredAt: now));

        return Result<Guid>.Success(invitation.Id);
    }
}
