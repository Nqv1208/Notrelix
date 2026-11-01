namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="Title">The new title (1 to 500 characters after trim).</param>
/// <param name="ExpectedVersion">The page version the client last read; a stale value answers 412.</param>
public record UpdatePageRequest(string Title, long ExpectedVersion);
