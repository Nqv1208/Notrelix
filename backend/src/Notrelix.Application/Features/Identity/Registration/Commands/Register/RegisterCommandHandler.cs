using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Application.Features.Identity.Registration.Services;
using Notrelix.Application.Features.Identity.Verification.Abstractions;

namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountProvisioningActions _provisioningActions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IRateLimitService _rateLimitService;
    private readonly IEmailVerificationTokenIssuer? _emailVerificationTokenIssuer;
    private readonly IEmailRegistrationProbe? _registrationProbe;
    private readonly RegistrationScope? _registrationScope;

    public RegisterCommandHandler(
        IIdentityDbContext identityContext,
        IAccountProvisioningActions provisioningActions,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector,
        IRateLimitService rateLimitService,
        IEmailVerificationTokenIssuer? emailVerificationTokenIssuer = null,
        IEmailRegistrationProbe? registrationProbe = null,
        RegistrationScope? registrationScope = null)
    {
        _registrationProbe = registrationProbe;
        _registrationScope = registrationScope;
        _identityContext = identityContext;
        _provisioningActions = provisioningActions;
        _passwordHasher = passwordHasher;
        _rateLimitService = rateLimitService;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventCollector = integrationEventCollector;
        _emailVerificationTokenIssuer = emailVerificationTokenIssuer;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = _registrationProbe is not null
            ? await _registrationProbe.IsRegisteredAsync(normalizedEmail, cancellationToken)
            : await _identityContext.Users
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // One neutral answer for a new and for an existing address: nothing in the response, the status or
        // (as far as the hash below levels it) the time says whether the address is registered. The person
        // behind an existing address is told by mail instead.
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        if (emailExists)
        {
            await NotifyExistingOwnerAsync(normalizedEmail, _dateTimeProvider.UtcNow, cancellationToken);
            return Result.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        var user = User.Create(request.Email, request.Name, passwordHash, now, hasPasswordCredential: true);
        if (_registrationScope is not null)
            await _registrationScope.EnterAsync(user.Id, cancellationToken);
        _identityContext.Users.Add(user);

        // Create personal account through the Accounts-owned public provisioning action
        var accountName = $"{request.Name}'s Account";
        var provisioning = await _provisioningActions.ProvisionPersonalAccountAsync(
            user.Id,
            request.Name,
            now,
            cancellationToken);
        var accountId = provisioning.AccountId;

        if (_emailVerificationTokenIssuer is not null)
        {
            await _emailVerificationTokenIssuer.IssueAsync(
                user,
                user.Id,
                now,
                cancellationToken);
        }

        // Emit registration completed use-case integration event
        _integrationEventCollector.Add(
            new IdentityRegistrationCompletedIntegrationEventV1(
                EventId: Guid.CreateVersion7(),
                UserId: user.Id,
                AccountId: accountId,
                Email: user.Email.Value,
                DisplayName: user.Name,
                AccountName: accountName,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: user.Id,
                SourceEventId: null,
                CausationId: null,
                OccurredAt: now));

        return Result.Success();
    }

    private async Task NotifyExistingOwnerAsync(
        string normalizedEmail,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Silent when limited: an answer that changed would show the address exists, and a loop of
        // registrations must not become a stream of mail to its owner.
        var limited = await _rateLimitService.IsRateLimitedAsync(
            "register-existing-notice", normalizedEmail, maxAttempts: 3, window: TimeSpan.FromHours(1));
        if (limited)
        {
            return;
        }

        // The mail itself is queued after the commit by a consumer: the request does the same work and
        // takes the same time whether or not the address is registered.
        var eventId = Guid.CreateVersion7();
        _integrationEventCollector.Add(
            new RegistrationAttemptedOnExistingAccountIntegrationEventV1(
                EventId: eventId,
                Email: normalizedEmail,
                CorrelationId: eventId,
                OccurredAt: now));
    }
}
