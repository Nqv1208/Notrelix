namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardSchemaDto(
    Guid Id,
    string Title,
    string? Description,
    List<BoardFieldSchemaDto> Fields,
    List<BoardGroupSchemaDto> Groups);
