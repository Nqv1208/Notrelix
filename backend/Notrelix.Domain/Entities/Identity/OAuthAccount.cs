using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Identity;

/// <summary>
/// Entity đại diện cho tài khoản SSO (Google, GitHub, Microsoft...)
/// </summary>
public class OAuthAccount : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ProviderId { get; private set; } = null!;
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public string RawProfile { get; private set; } = "{}";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private OAuthAccount() : base() { }

    public static OAuthAccount Create(
        Guid userId,
        string provider,
        string providerId,
        string? accessToken = null,
        string? refreshToken = null,
        DateTime? tokenExpiresAt = null,
        string rawProfile = "{}")
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider không được để trống", nameof(provider));

        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID không được để trống", nameof(providerId));

        return new OAuthAccount
        {
            UserId = userId,
            Provider = provider.Trim().ToLowerInvariant(),
            ProviderId = providerId.Trim(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt,
            RawProfile = string.IsNullOrWhiteSpace(rawProfile) ? "{}" : rawProfile,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTokens(string? accessToken, string? refreshToken, DateTime? expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsTokenExpired => TokenExpiresAt.HasValue && TokenExpiresAt.Value < DateTime.UtcNow;
}
