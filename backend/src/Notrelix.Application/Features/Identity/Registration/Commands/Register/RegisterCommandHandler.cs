using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountProvisioningService _provisioningService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IEmailVerificationTokenIssuer? _emailVerificationTokenIssuer;

    public RegisterCommandHandler(
        IIdentityDbContext identityContext,
        IAccountProvisioningService provisioningService,
        IPasswordHasher passwordHasher,
        IAuthSessionIssuer sessionIssuer,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector,
        IEmailVerificationTokenIssuer? emailVerificationTokenIssuer = null)
    {
        _identityContext = identityContext;
        _provisioningService = provisioningService;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventCollector = integrationEventCollector;
        _emailVerificationTokenIssuer = emailVerificationTokenIssuer;
    }

    public async Task<Result<AuthResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _identityContext.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<AuthResult>.Failure("Email is already in use");
        }

        var now = _dateTimeProvider.UtcNow;
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, request.Name, passwordHash, now);
        _identityContext.Users.Add(user);

        // Create personal account through the Accounts-owned provisioning service
        var accountName = $"{request.Name}'s Account";
        var provisioning = await _provisioningService.ProvisionPersonalAccountAsync(
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

        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return Result<AuthResult>.Success(authResult with { WorkspaceProvisioning = "pending" });
    }
}
