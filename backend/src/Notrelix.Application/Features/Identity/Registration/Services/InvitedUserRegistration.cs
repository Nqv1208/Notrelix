using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.InvitedRegistration;

namespace Notrelix.Application.Features.Identity.Registration.Services;

/// <summary>
/// Registration of a person who holds a one-time Workspace invitation for their address. Mirrors
/// self-service registration (personal Account, registration event, session) except that the email
/// is confirmed at once: the invitation token was delivered to it. Runs inside the caller's request
/// transaction.
/// </summary>
public sealed class InvitedUserRegistrationActions : IInvitedUserRegistrationActions
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountProvisioningActions _provisioningActions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    public InvitedUserRegistrationActions(
        IIdentityDbContext identityContext,
        IAccountProvisioningActions provisioningActions,
        IPasswordHasher passwordHasher,
        IAuthSessionIssuer sessionIssuer,
        IIntegrationEventCollector integrationEventCollector)
    {
        _identityContext = identityContext;
        _provisioningActions = provisioningActions;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _integrationEventCollector = integrationEventCollector;
    }

    public async Task<InvitedUserRegistration?> RegisterAsync(
        string email,
        string name,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var emailExists = await _identityContext.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
            return null;

        var user = User.Create(email, name, _passwordHasher.HashPassword(password), now, hasPasswordCredential: true);
        user.ConfirmEmail(null, now);
        _identityContext.Users.Add(user);

        var accountName = $"{name.Trim()}'s Account";
        var provisioning = await _provisioningActions.ProvisionPersonalAccountAsync(
            user.Id,
            name,
            now,
            cancellationToken);

        _integrationEventCollector.Add(
            new IdentityRegistrationCompletedIntegrationEventV1(
                EventId: Guid.CreateVersion7(),
                UserId: user.Id,
                AccountId: provisioning.AccountId,
                Email: user.Email.Value,
                DisplayName: user.Name,
                AccountName: accountName,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: user.Id,
                SourceEventId: null,
                CausationId: null,
                OccurredAt: now));

        var session = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return new InvitedUserRegistration(
            user.Id,
            user.Email.Value,
            user.Name,
            session.AccessToken!,
            session.RefreshToken!,
            session.ExpiresAt!.Value);
    }
}
