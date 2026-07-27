using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token)
    : ICommand<Result<ConfirmEmailResultDto>>, IAnonymousTokenScopedRequest, ITransactionalRequest
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
    private readonly IIdentityDbContext _identityContext;
    private readonly IOneTimeTokenService _oneTimeTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmEmailCommandHandler(
        IIdentityDbContext identityContext,
        IOneTimeTokenService oneTimeTokenService,
        IDateTimeProvider dateTimeProvider)
    {
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

        var token = await _identityContext.EmailVerificationTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash.Value == presentedHash.TokenHash
                    && t.HashVersion == presentedHash.HashVersion
                    && t.Status == UserTokenStatus.Active,
                cancellationToken);

        if (token is null)
            return Result<ConfirmEmailResultDto>.Failure("Invalid or expired verification token.");

        var now = _dateTimeProvider.UtcNow;

        if (now >= token.ExpiresAt)
        {
            token.Expire(now);
            return Result<ConfirmEmailResultDto>.Failure("Verification token has expired.");
        }

        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

        if (user is null)
            return Result<ConfirmEmailResultDto>.Failure("User not found.");

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
}
