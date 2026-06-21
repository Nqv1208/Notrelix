namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardViewDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string Type,
    string Config,
    bool IsDefault);
