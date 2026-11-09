namespace Notrelix.Application.Features.Governance.DTOs;

/// <summary>Link metadata only. The token and its hash never leave the server (Lead decision L6).</summary>
public record ShareLinkDto(
    Guid Id,
    Guid WorkspaceId,
    string ResourceKind,
    Guid ResourceId,
    string AccessMode,
    string Level,
    bool IsEnabled,
    DateTimeOffset? ExpiresAt
);
