namespace Notrelix.Infrastructure.Auth.Credentials;

/// <summary>
/// Resolves the authenticated credential context from trusted claims on the
/// current HTTP principal. Only claims emitted by trusted authentication
/// handlers (e.g. the API token handler) are consumed.
/// </summary>
public class CurrentCredentialContext : ICurrentCredentialContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentCredentialContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    private string? GetClaim(string type) => Principal?.FindFirst(type)?.Value;

    public CredentialKind Kind => GetClaim(CredentialClaims.Kind) switch
    {
        CredentialClaims.ApiTokenKindValue => CredentialKind.ApiToken,
        _ => CredentialKind.None
    };

    public Guid? ApiTokenId => TryParseGuid(GetClaim(CredentialClaims.ApiTokenId));

    public Guid? BoundAccountId => TryParseGuid(GetClaim(CredentialClaims.AccountId));

    public Guid? BoundWorkspaceId => TryParseGuid(GetClaim(CredentialClaims.WorkspaceId));

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var id) ? id : null;
}