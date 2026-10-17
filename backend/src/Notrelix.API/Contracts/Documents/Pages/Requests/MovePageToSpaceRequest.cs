namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="SpaceId">Target Space for the root page and its subtree; null moves them back to the workspace root.</param>
/// <param name="ExpectedVersion">The root page version the client last read.</param>
public record MovePageToSpaceRequest(Guid? SpaceId, long ExpectedVersion);
