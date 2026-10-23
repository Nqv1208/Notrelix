namespace Notrelix.Domain.Identity.Security;

/// <summary>
/// One immutable line of a user's security activity feed (Identity owns the fact). It is
/// user-visible history, separate from the operator security audit. It is only ever created
/// for an existing account and never carries a secret, an attempted password or an email address.
/// </summary>
public sealed class SecurityActivityEntry
{
    public const int IpAddressMaxLength = 45;
    public const int UserAgentMaxLength = 512;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SecurityActivityType Type { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? SessionId { get; private set; }

    private SecurityActivityEntry() { }

    public static SecurityActivityEntry Record(
        Guid userId,
        SecurityActivityType type,
        DateTimeOffset occurredAt,
        string? ipAddress = null,
        string? userAgent = null,
        Guid? sessionId = null)
    {
        Guard.NotEmpty(userId);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown security activity type.");
        }

        return new SecurityActivityEntry
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            OccurredAt = occurredAt,
            IpAddress = Truncate(ipAddress, IpAddressMaxLength),
            UserAgent = Truncate(userAgent, UserAgentMaxLength),
            SessionId = sessionId
        };
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
