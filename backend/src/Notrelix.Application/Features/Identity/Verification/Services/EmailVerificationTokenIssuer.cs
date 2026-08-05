using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Verification.Services;

public sealed class EmailVerificationTokenIssuer : IEmailVerificationTokenIssuer
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IActiveVerificationTokenLocker _tokenLocker;
    private readonly IOneTimeTokenService _tokenService;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    public EmailVerificationTokenIssuer(
        IIdentityDbContext identityContext,
        IActiveVerificationTokenLocker tokenLocker,
        IOneTimeTokenService tokenService,
        ISecretEncryptor secretEncryptor,
        IIntegrationEventCollector integrationEventCollector)
    {
        _identityContext = identityContext;
        _tokenLocker = tokenLocker;
        _tokenService = tokenService;
        _secretEncryptor = secretEncryptor;
        _integrationEventCollector = integrationEventCollector;
    }

    public async Task<EmailVerificationTokenIssue> IssueAsync(
        User user,
        Guid actorUserId,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _tokenLocker.LockActiveTokensAsync(user.Id, cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(issuedAt, "superseded-by-new-email-verification");
        }

        var issued = _tokenService.Generate(TokenPurpose.EmailVerification);
        var token = EmailVerificationToken.Create(
            user.Id,
            TokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            user.Email.Value,
            issuedAt.AddHours(1),
            issuedAt);

        _identityContext.EmailVerificationTokens.Add(token);

        var eventId = Guid.CreateVersion7();
        var protectedToken = _secretEncryptor.Protect(
            issued.RawToken,
            OneTimeTokenProtectionPurposes.EmailVerification);
        _integrationEventCollector.Add(
            new EmailVerificationDeliveryRequestedIntegrationEventV1(
                EventId: eventId,
                VerificationTokenId: token.Id,
                UserId: user.Id,
                Email: user.Email.Value,
                ProtectedToken: protectedToken,
                HashVersion: issued.HashVersion,
                ExpiresAt: token.ExpiresAt,
                CorrelationId: eventId,
                ActorUserId: actorUserId,
                OccurredAt: issuedAt));

        return new EmailVerificationTokenIssue(
            token.Id,
            user.Id,
            user.Email.Value,
            protectedToken,
            issued.HashVersion,
            token.ExpiresAt);
    }
}
