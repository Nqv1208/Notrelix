namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="AddUserIds">People to add as board members who can edit before the board becomes Private; may be empty.</param>
public record MakeBoardPrivateRequest(IReadOnlyList<Guid>? AddUserIds);
