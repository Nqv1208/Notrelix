namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// What a copy takes from its source besides name, dates and description. Everything is opt-in; comments,
/// history, files and links are never copied (files belong to Collaboration and are not duplicated yet).
/// </summary>
public sealed record DuplicateBoardItemOptions(
    bool IncludeFields = false,
    bool IncludeChecklists = false,
    bool IncludeSubItems = false,
    bool IncludeLabels = false,
    bool IncludeAssignees = false);
