namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>Optional body of a duplicate: the copy's name and what it takes along. No body copies name, dates and description only.</summary>
public record DuplicateBoardItemRequest(
    string? Name = null,
    bool IncludeFields = false,
    bool IncludeChecklists = false,
    bool IncludeSubItems = false,
    bool IncludeLabels = false,
    bool IncludeAssignees = false);
