namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="ParentId">The page to put the page under; null makes it a root page of its Space.</param>
/// <param name="ExpectedVersion">The page version the client last read; a stale value answers 412.</param>
public record MovePageRequest(Guid? ParentId, long ExpectedVersion);
