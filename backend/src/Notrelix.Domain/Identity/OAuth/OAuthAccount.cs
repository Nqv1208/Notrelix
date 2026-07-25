namespace Notrelix.Domain.Identity.OAuth;

public class OAuthAccount : Entity
{
    public Guid UserId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderId { get; private set; } = null!;
    public OAuthToken? Token { get; private set; }
    public OAuthProfileSnapshot ProfileSnapshot { get; private set; } = null!;

    private OAuthAccount() : base() { }

    public static OAuthAccount Create(
        Guid userId,
        OAuthProvider provider,
        string providerId,
        OAuthProfileSnapshot profileSnapshot,
        OAuthToken? token = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(providerId);
        Guard.NotNull(profileSnapshot);

        return new OAuthAccount
        {
            UserId = userId,
            Provider = provider,
            ProviderId = providerId.Trim(),
            Token = token,
            ProfileSnapshot = profileSnapshot
        };
    }

    internal void UpdateToken(OAuthToken token)
    {
        Guard.NotNull(token);
        Token = token;
    }

    internal void UpdateProfileSnapshot(OAuthProfileSnapshot profileSnapshot)
    {
        Guard.NotNull(profileSnapshot);
        ProfileSnapshot = profileSnapshot;
    }
}
