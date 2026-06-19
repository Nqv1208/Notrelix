namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardFieldDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string FieldType,
    string Settings,
    string? DefaultValue,
    string Position,
    bool IsSystem,
    bool IsDeleted
);

public record BoardFieldSchemaDto(
    Guid Id,
    string Name,
    string Type,
    string Settings,
    string? DefaultValue,
    string Position,
    bool IsSystem);
