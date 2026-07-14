using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;
    private readonly IEmailVerificationTokenIssuer? _emailVerificationTokenIssuer;

    public RegisterCommandHandler(
        IIdentityDbContext identityContext,
        IAccountDbContext accountContext,
        IPasswordHasher passwordHasher,
        IAuthSessionIssuer sessionIssuer,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector,
        IEmailVerificationTokenIssuer? emailVerificationTokenIssuer = null)
    {
        _identityContext = identityContext;
        _accountContext = accountContext;
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

        // Create personal account
        var accountSlug = Slug.GenerateFromName($"{request.Name}'s Account");
        var account = Account.Create(
            $"{request.Name}'s Account",
            accountSlug.Value,
            AccountType.Personal,
            user.Id,
            now);
        _accountContext.Accounts.Add(account);

        // Create account member (owner)
        var accountMember = AccountMember.Create(
            account.Id,
            user.Id,
            AccountRole.Owner,
            user.Id,
            now);
        _accountContext.AccountMembers.Add(accountMember);

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
                AccountId: account.Id,
                Email: user.Email.Value,
                DisplayName: user.Name,
                AccountName: account.Name,
                CorrelationId: Guid.CreateVersion7(),
                ActorUserId: user.Id,
                SourceEventId: null,
                CausationId: null,
                OccurredAt: now));

        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return Result<AuthResult>.Success(authResult with { WorkspaceProvisioning = "pending" });
    }
}
