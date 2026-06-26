namespace Notrelix.Domain.Identity.OAuth;

public class OAuthAccount : Entity
{
    public Guid UserId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderId { get; private set; } = null!;
    public OAuthToken? Token { get; private set; }
    public JsonValue RawProfile { get; private set; } = null!;

    private OAuthAccount() : base() { }

    public static OAuthAccount Create(
        Guid userId,
        OAuthProvider provider,
        string providerId,
        JsonValue rawProfile,
        OAuthToken? token = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(providerId);
        Guard.NotNull(rawProfile);

        return new OAuthAccount
        {
            UserId = userId,
            Provider = provider,
            ProviderId = providerId.Trim(),
            Token = token,
            RawProfile = rawProfile
        };
    }

    internal void UpdateToken(OAuthToken token)
    {
        Guard.NotNull(token);
        Token = token;
    }
}
