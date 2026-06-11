using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public sealed class OAuthToken : ValueObject
{
    public string AccessTokenRef { get; }
    public string? RefreshTokenRef { get; }
    public DateTimeOffset? ExpiresAt { get; }

    private OAuthToken(string accessTokenRef, string? refreshTokenRef, DateTimeOffset? expiresAt)
    {
        AccessTokenRef = accessTokenRef;
        RefreshTokenRef = refreshTokenRef;
        ExpiresAt = expiresAt;
    }

    public static OAuthToken Create(string accessTokenRef, string? refreshTokenRef = null, DateTimeOffset? expiresAt = null)
    {
        Guard.NotNullOrWhiteSpace(accessTokenRef);
        return new OAuthToken(accessTokenRef, refreshTokenRef, expiresAt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccessTokenRef;
        yield return RefreshTokenRef;
        yield return ExpiresAt;
    }
}
