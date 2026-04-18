using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities;

// Entity đại diện cho phiên đăng nhập của user
public class Session : BaseEntity
{
    public Guid UserId { get; private set; }
    public string RefreshToken { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private Session() : base() { }

    public static Session Create(Guid userId, TimeSpan expiration, string? deviceInfo = null, string? ipAddress = null)
    {
        return new Session
        {
            UserId = userId,
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
    }

    public static Session Create(Guid userId, string refreshToken, DateTime expiresAt, string? deviceInfo = null, string? ipAddress = null)
    {
        return new Session
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
    }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke()
    {
        if (!IsRevoked)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
    }

    public void Refresh(TimeSpan expiration)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Cannot refresh a revoked session");

        RefreshToken = GenerateRefreshToken();
        ExpiresAt = DateTime.UtcNow.Add(expiration);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
