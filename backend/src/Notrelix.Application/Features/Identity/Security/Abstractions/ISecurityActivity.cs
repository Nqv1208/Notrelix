using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Security.Abstractions;

/// <summary>
/// Records one line of the user's own security activity feed. The entry is only staged: it is
/// persisted by the request data session's <c>SaveChanges</c>, in the same transaction as the
/// state change it describes (so a rolled-back request leaves no line, and a committed one cannot
/// lose it). Client IP and user agent come from the current request. Callers record only for an
/// existing account, never for an unknown email, and never pass a secret.
/// </summary>
public interface ISecurityActivityRecorder
{
    void Record(Guid userId, SecurityActivityType type, DateTimeOffset occurredAt, Guid? sessionId = null);
}

/// <summary>Read side of the security activity feed. Always scoped to one user.</summary>
public interface ISecurityActivityReader
{
    /// <summary>
    /// Newest first (occurred-at then id, both descending). When <paramref name="after"/> is given only
    /// entries strictly older than that position are returned.
    /// </summary>
    Task<IReadOnlyList<SecurityActivityDto>> ListAsync(
        Guid userId,
        SecurityActivityPosition? after,
        int take,
        CancellationToken cancellationToken);
}

/// <summary>Keyset position of an entry in the feed ordering.</summary>
public readonly record struct SecurityActivityPosition(DateTimeOffset OccurredAt, Guid Id);

/// <summary>
/// One security activity line as exposed to its owner. <c>SessionId</c> is present when the event
/// concerns one session (sign-in success, a single session revoked).
/// </summary>
public sealed record SecurityActivityDto(
    Guid Id,
    SecurityActivityType Type,
    DateTimeOffset OccurredAt,
    string? IpAddress,
    string? UserAgent,
    Guid? SessionId);
