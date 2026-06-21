using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public sealed class OAuthToken : ValueObject
{
    public SecretRef AccessTokenRef { get; }
    public SecretRef? RefreshTokenRef { get; }
    public DateTimeOffset? ExpiresAt { get; }

    private OAuthToken() { }    private OAuthToken(SecretRef accessTokenRef, SecretRef? refreshTokenRef, DateTimeOffset? expiresAt)
    {
        AccessTokenRef = accessTokenRef;
        RefreshTokenRef = refreshTokenRef;
        ExpiresAt = expiresAt;
    }

    public static OAuthToken Create(SecretRef accessTokenRef, SecretRef? refreshTokenRef = null, DateTimeOffset? expiresAt = null)
    {
        Guard.NotNull(accessTokenRef);
        return new OAuthToken(accessTokenRef, refreshTokenRef, expiresAt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccessTokenRef;
        yield return RefreshTokenRef;
        yield return ExpiresAt;
    }
}
