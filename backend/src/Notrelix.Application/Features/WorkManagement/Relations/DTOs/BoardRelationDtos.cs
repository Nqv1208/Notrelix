namespace Notrelix.Application.Features.WorkManagement.Relations.DTOs;

public record BoardRelationDto(
    Guid Id,
    Guid SourceBoardId,
    Guid TargetBoardId,
    string RelationType,
    string Direction,
    string SyncMode,
    string Status
);
