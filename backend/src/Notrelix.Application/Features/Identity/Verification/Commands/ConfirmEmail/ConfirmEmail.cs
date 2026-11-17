using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token)
    : ICommand<Result<ConfirmEmailResultDto>>, IAnonymousTokenScopedRequest, IWriteRequest
{
    TokenPurpose ITokenScopedRequest.TokenPurpose =>
        TokenPurpose.EmailVerification;
}

public sealed record ConfirmEmailResultDto(
    bool EmailConfirmed,
    bool SessionRefreshRequired);

public sealed class ConfirmEmailCommandHandler
    : IRequestHandler<ConfirmEmailCommand, Result<ConfirmEmailResultDto>>
{
    /// <summary>Public Identity error code of a link that was valid and has run out (answered as 400).</summary>
    public const string TokenExpiredCode = "identity.email-verification.token-expired";

    private readonly IIdentityDbContext _identityContext;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;
    private readonly CredentialScope? _credentialScope;
    private readonly IEmailRegistrationProbe? _registrationProbe;

    private static readonly TimeSpan SessionMarkerTtl = TimeSpan.FromHours(24);

    public ConfirmEmailCommandHandler(
        IIdentityDbContext identityContext,
        IOneTimeTokenService oneTimeTokenService,
        IDateTimeProvider dateTimeProvider,
        IJwtBlacklistService jwtBlacklist,
        IEmailService emailService,
        ILogger<ConfirmEmailCommandHandler> logger,
        CredentialScope? credentialScope = null,
        IEmailRegistrationProbe? registrationProbe = null)
    {
        _jwtBlacklist = jwtBlacklist;
        _emailService = emailService;
        _logger = logger;
        _registrationProbe = registrationProbe;
        _credentialScope = credentialScope;
        _identityContext = identityContext;
        _oneTimeTokenService = oneTimeTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ConfirmEmailResultDto>> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        ParsedOneTimeToken presentedHash;
        try
        {
            presentedHash = _oneTimeTokenService.ParseAndHash(
                request.Token,
                TokenPurpose.EmailVerification);
        }
        catch (InvalidOneTimeTokenException)
        {
            return Result<ConfirmEmailResultDto>.Failure("Invalid or expired verification token.");
        }

        // The credential scope is bound to the one user this token digest belongs to.
        if (_credentialScope is not null)
            await _credentialScope.EnterByEmailVerificationAsync(
                presentedHash.TokenHash, presentedHash.HashVersion, cancellationToken);

        var presentedTokenHash = TokenHash.Create(presentedHash.TokenHash);
        var token = await _identityContext.EmailVerificationTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == presentedTokenHash
                    && t.HashVersion == presentedHash.HashVersion
                    && t.Status == UserTokenStatus.Active,
                cancellationToken);

        if (token is null)
            return Result<ConfirmEmailResultDto>.Failure("Invalid or expired verification token.");

        var now = _dateTimeProvider.UtcNow;

        if (now >= token.ExpiresAt)
        {
            token.Expire(now);
            // Only the holder of a live link learns it ran out; an unknown or used token keeps the generic answer.
            return Result<ConfirmEmailResultDto>.Failure(new ApplicationError(
                TokenExpiredCode, "Verification token has expired.", ApplicationErrorType.BusinessRule));
        }

        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

        if (user is null)
            return Result<ConfirmEmailResultDto>.Failure("User not found.");

        // The link of a sign-in email change names the address the person parked, not the current one.
        if (user.PendingEmail is not null
            && string.Equals(token.NormalizedEmailSnapshot, user.PendingEmail, StringComparison.Ordinal))
            return await ConfirmChangeAsync(user, token, now, cancellationToken);

        if (string.IsNullOrWhiteSpace(token.NormalizedEmailSnapshot)
            || !string.Equals(
                token.NormalizedEmailSnapshot,
                user.NormalizedEmail,
                StringComparison.Ordinal))
        {
            return Result<ConfirmEmailResultDto>.Failure(
                "Invalid or expired verification token.");
        }

        token.MarkUsed(now);
        user.ConfirmEmail(null, now);

        return Result<ConfirmEmailResultDto>.Success(
            new ConfirmEmailResultDto(
                EmailConfirmed: user.EmailConfirmed,
                SessionRefreshRequired: true));
    }

    private async Task<Result<ConfirmEmailResultDto>> ConfirmChangeAsync(
        User user, EmailVerificationToken token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Someone may have registered the address while the link waited; the person sees the generic refusal.
        if (_registrationProbe is not null
            && await _registrationProbe.IsRegisteredAsync(user.PendingEmail!, cancellationToken))
        {
            return Result<ConfirmEmailResultDto>.Failure("Invalid or expired verification token.");
        }

        var oldEmail = user.Email.Value;
        token.MarkUsed(now);
        user.ConfirmEmailChange(user.Id, now);

        // The link may be opened on any device, so every session ends and the person signs in with the new address.
        var sessions = await _identityContext.Sessions
            .Where(s => s.UserId == user.Id && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now, SessionRevocationReasons.EmailChanged);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionMarkerTtl);
        }

        try
        {
            await _emailService.SendAsync(
                oldEmail,
                "Sign-in email changed — Notrelix",
                EmailTemplateService.EmailChange(user.Name, user.Email.Value, confirmed: true),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the email changed notice to {UserId}", user.Id);
        }

        return Result<ConfirmEmailResultDto>.Success(
            new ConfirmEmailResultDto(EmailConfirmed: true, SessionRefreshRequired: true));
    }
}
