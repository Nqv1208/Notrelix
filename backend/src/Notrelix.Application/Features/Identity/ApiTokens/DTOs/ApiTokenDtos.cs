namespace Notrelix.Application.Features.Identity.ApiTokens.DTOs;

/// <summary>Token metadata returned by list/read operations. The raw secret is never included.</summary>
public sealed record ApiTokenSummaryDto(
    Guid Id,
    string Name,
    string? Scopes,
    string Status,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt,
    /// <summary>The last four characters of the secret; null for tokens issued before it was kept.</summary>
    string? Suffix = null,
    /// <summary>Why a revoked token stopped (Manual, MemberRemoved, MemberSuspended, MemberLeft); null while it is not revoked or for one revoked before the reason was kept.</summary>
    string? RevokedReason = null);

/// <summary>Issuance result. Contains the raw secret — the only time it is ever returned.</summary>
public sealed record CreatedApiTokenDto(
    Guid Id,
    string RawSecret,
    string Name,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);