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
    bool IsDeleted,
    long Version,
    // Options of a Select or Status field in display order; null for other field types.
    List<BoardFieldOptionDto>? Options = null,
    // Live Items of the Board that hold a non-empty value of this field; 0 when none do.
    int ValueCount = 0
);

/// <summary>
/// One option of a Select or Status field. <see cref="Category"/> is the
/// work-status category of a Status option ("todo", "in_progress", "review",
/// "done"); null for Select options.
/// </summary>
public record BoardFieldOptionDto(
    Guid Id,
    string Name,
    string Color,
    string Position,
    string? Category);

public record BoardFieldSchemaDto(
    Guid Id,
    string Name,
    string Type,
    string Settings,
    string? DefaultValue,
    string Position,
    bool IsSystem,
    long Version);
