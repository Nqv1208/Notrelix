namespace Notrelix.Application.Features.WorkManagement.Templates.DTOs;

public record BoardTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Status
);
