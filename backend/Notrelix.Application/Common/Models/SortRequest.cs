namespace Notrelix.Application.Common.Models;

public record SortRequest(
    string Field,
    bool Descending = false
);
