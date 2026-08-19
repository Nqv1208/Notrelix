using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Infrastructure.Auth.Credentials;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Security.ApiTokens;

namespace Notrelix.Infrastructure.Auth.ApiTokens;

/// <summary>
/// Authenticates requests presenting an API token as "Authorization: Bearer ntk_v1.&lt;secret&gt;".
/// The presented secret is hashed and looked up by digest; verification fails closed on
/// unknown, revoked, expired or user-less tokens. The lookup runs in the system context
/// (single-row credential check by unguessable digest — same trust model as the JWT
/// blacklist), while all authorized application access still flows through RLS.
/// </summary>
public sealed class ApiTokenAuthenticationHandler : AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IApiTokenSecretService _secretService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<ApiTokenAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        DbContextOptions<ApplicationDbContext> optionsAccessor,
        IApiTokenSecretService secretService,
        IDateTimeProvider dateTimeProvider)
        : base(options, logger, encoder, clock)
    {
        _options = optionsAccessor;
        _secretService = secretService;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var presented = header["Bearer ".Length..].Trim();
        if (presented.Length == 0 || presented.Length > ApiTokenSecretService.MaximumTokenLength)
        {
            return AuthenticateResult.NoResult();
        }

        if (!presented.StartsWith(ApiTokenSecretService.ApiTokenPrefix, StringComparison.Ordinal) ||
            presented.Length == ApiTokenSecretService.ApiTokenPrefix.Length)
        {
            return AuthenticateResult.NoResult();
        }

        var hash = _secretService.Hash(presented);
        var now = _dateTimeProvider.UtcNow;

        var tenant = new CurrentTenantContext();
        tenant.SetSystem();
        await using var context = new ApplicationDbContext(_options, tenant);

        var token = await context.ApiTokens.SingleOrDefaultAsync(
            t => t.TokenHash == hash, Context.RequestAborted);

        if (token is null ||
            token.Status != ApiTokenStatus.Active ||
            token.UserId is null ||
            (token.ExpiresAt.HasValue && token.ExpiresAt.Value <= now))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            token.RecordUse(now);
            await context.SaveChangesAsync(Context.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another writer (e.g. revocation) committed the token state; the
            // credential may no longer be authoritative. Fail closed rather
            // than authenticating against a raced read.
            return AuthenticateResult.NoResult();
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, token.UserId.Value.ToString()),
                new Claim(CredentialClaims.Kind, CredentialClaims.ApiTokenKindValue),
                new Claim(CredentialClaims.ApiTokenId, token.Id.ToString()),
                new Claim(CredentialClaims.AccountId, token.AccountId.ToString()),
                new Claim(CredentialClaims.WorkspaceId, token.WorkspaceId.ToString())
            ],
            ApiTokenAuthenticationOptions.SchemeName);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), ApiTokenAuthenticationOptions.SchemeName));
    }
}