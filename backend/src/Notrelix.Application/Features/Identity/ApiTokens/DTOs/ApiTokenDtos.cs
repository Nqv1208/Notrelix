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
    DateTimeOffset? RevokedAt);

/// <summary>Issuance result. Contains the raw secret — the only time it is ever returned.</summary>
public sealed record CreatedApiTokenDto(
    Guid Id,
    string RawSecret,
    string Name,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);