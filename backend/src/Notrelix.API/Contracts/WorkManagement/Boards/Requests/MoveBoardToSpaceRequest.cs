namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="SpaceId">Target Space; null moves the board back to the workspace root.</param>
/// <param name="ExpectedVersion">The board version the client last read.</param>
public record MoveBoardToSpaceRequest(Guid? SpaceId, long ExpectedVersion);
