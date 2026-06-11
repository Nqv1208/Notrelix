using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public class OAuthAccount : Entity
{
    public Guid UserId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderId { get; private set; } = null!;
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? TokenExpiresAt { get; private set; }
    public JsonValue RawProfile { get; private set; } = null!;

    private OAuthAccount() : base() { }

    public static OAuthAccount Create(
        Guid userId,
        OAuthProvider provider,
        string providerId,
        JsonValue rawProfile,
        string? accessToken = null,
        string? refreshToken = null,
        DateTimeOffset? tokenExpiresAt = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(providerId);

        return new OAuthAccount
        {
            UserId = userId,
            Provider = provider,
            ProviderId = providerId.Trim(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt,
            RawProfile = rawProfile
        };
    }
}
