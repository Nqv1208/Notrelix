namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="SpaceId">Target Space; null moves the board back to the workspace root.</param>
/// <param name="ExpectedVersion">The board version the client last read.</param>
/// <param name="AddUserIds">People to add to the board as editors in the same step, so they keep it after the move.</param>
public record MoveBoardToSpaceRequest(Guid? SpaceId, long ExpectedVersion, IReadOnlyList<Guid>? AddUserIds = null);
