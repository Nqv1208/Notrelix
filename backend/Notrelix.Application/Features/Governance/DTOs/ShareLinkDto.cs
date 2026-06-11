namespace Notrelix.Application.Features.Governance.DTOs;

public record ShareLinkDto(
    Guid Id,
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string TokenHash,
    string Level,
    bool IsEnabled,
    DateTime? ExpiresAt
);
