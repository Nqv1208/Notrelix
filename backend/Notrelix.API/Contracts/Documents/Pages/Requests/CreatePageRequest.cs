namespace Notrelix.API.Contracts.Documents.Pages.Requests;

public record CreatePageRequest(string Title, Guid? ParentId = null);
