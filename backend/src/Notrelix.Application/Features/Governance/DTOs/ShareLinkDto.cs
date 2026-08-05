namespace Notrelix.Application.Features.Governance.DTOs;

public record ShareLinkDto(
    Guid Id,
    Guid WorkspaceId,
    string ResourceKind,
    Guid ResourceId,
    string TokenHash,
    string AccessMode,
    bool IsEnabled,
    DateTimeOffset? ExpiresAt
);
