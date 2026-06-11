namespace Notrelix.Application.Common.Models;

public record CursorRequest(
    string? Cursor = null,
    int Limit = 20
);
