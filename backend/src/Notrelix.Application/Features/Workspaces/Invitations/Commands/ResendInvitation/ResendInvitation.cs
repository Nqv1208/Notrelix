using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(
    Guid WorkspaceId,
    Guid InvitationId)
    : ICommand<Result<Guid>>,
      ITransactionalRequest,
      IWorkspaceRequest,
      IRequirePermission,
      IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
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

    public ResendInvitationCommandHandler(
        IWorkspaceDbContext workspaceContext,
        ICurrentRequestContext requestContext,
        IOneTimeTokenService tokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector,
        IDateTimeProvider dateTimeProvider)
    {
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
                OccurredAt: now));

        return Result<Guid>.Success(invitation.Id);
    }
}
