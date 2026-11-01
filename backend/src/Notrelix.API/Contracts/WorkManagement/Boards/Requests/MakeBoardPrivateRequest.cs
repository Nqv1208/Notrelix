namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="AddUserIds">People to add as board members who can edit before the board becomes Private; may be empty.</param>
/// <param name="AddMeAsOwner">The caller chooses to become the board's Owner (only when it has none and they hold no board role); never implicit.</param>
public record MakeBoardPrivateRequest(IReadOnlyList<Guid>? AddUserIds, bool AddMeAsOwner = false);
