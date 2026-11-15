namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="Title">The new title (1 to 500 characters after trim); omit to keep it.</param>
/// <param name="ExpectedVersion">The page version the client last read; a stale value answers 412.</param>
/// <param name="Icon">One emoji or token (32 characters at most); empty restores the default; omit to keep it.</param>
public record UpdatePageRequest(string? Title, long ExpectedVersion, string? Icon = null);
