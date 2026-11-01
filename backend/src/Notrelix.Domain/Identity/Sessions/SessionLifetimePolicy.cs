namespace Notrelix.Domain.Identity.Sessions;

/// <summary>
/// Platform values for how long a session lives. An Account policy may shorten them later (Accounts);
/// nothing here may be loosened per request.
/// </summary>
public static class SessionLifetimePolicy
{
    /// <summary>A session unused for this long ends; every refresh starts the period again.</summary>
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromDays(30);

    /// <summary>A session ends this long after sign-in however often it is used.</summary>
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(90);

    /// <summary>
    /// How long the credential a rotation replaced still answers with the new one: long enough for two
    /// tabs refreshing together or a retry after a lost response, too short to help a thief.
    /// </summary>
    public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(10);

    /// <summary>The expiry after a refresh at <paramref name="now"/>: idle period, capped by the absolute limit.</summary>
    public static DateTimeOffset ExpiryAfterRefresh(DateTimeOffset createdAt, DateTimeOffset now)
    {
        var idle = now + IdleLifetime;
        var absolute = createdAt + AbsoluteLifetime;
        return idle < absolute ? idle : absolute;
    }
}
