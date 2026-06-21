namespace Notrelix.API.Contracts.Documents.Pages.Requests;

public record UpdatePageRequest(string? Title, string? IconType, string? IconValue, string? CoverUrl);
