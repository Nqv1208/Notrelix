namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="Title">Title of the copy; omitted, it is "Copy of" the original title.</param>
public record DuplicatePageRequest(string? Title = null);
