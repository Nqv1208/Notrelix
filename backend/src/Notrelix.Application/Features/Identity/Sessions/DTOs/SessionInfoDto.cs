namespace Notrelix.Application.Features.Identity.Sessions.DTOs;

public sealed record SessionInfoDto(
    Guid Id,
    SessionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? IpAddress,
    string? UserAgent,
    bool IsCurrent);