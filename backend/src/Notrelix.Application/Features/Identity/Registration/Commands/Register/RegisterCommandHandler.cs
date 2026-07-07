using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Identity.Registration.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    public RegisterCommandHandler(
        IIdentityDbContext identityContext,
        IAccountDbContext accountContext,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventCollector integrationEventCollector)
    {
        _identityContext = identityContext;
        _accountContext = accountContext;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventCollector = integrationEventCollector;
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

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = RefreshTokenHash.Create(refreshToken);

        var session = UserSession.Create(user.Id, tokenHash, now.AddDays(30), now);
        _identityContext.Sessions.Add(session);

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

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(1).DateTime,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl
            },
            WorkspaceProvisioning = "pending"
        });
    }
}
