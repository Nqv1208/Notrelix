namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardViewDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string Type,
    string Config,
    bool IsDefault,
    string Visibility = "Board",
    Guid? CreatedBy = null,
    long Version = 0)
{
    public static BoardViewDto From(BoardView view) => new(
        view.Id,
        view.BoardId,
        view.Name,
        view.Type.ToString(),
        view.Config.Data.Value,
        view.IsDefault,
        view.Visibility.ToString(),
        view.CreatedBy,
        view.Version);
}
