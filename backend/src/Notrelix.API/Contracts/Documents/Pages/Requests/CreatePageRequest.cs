namespace Notrelix.API.Contracts.Documents.Pages.Requests;

/// <param name="ParentId">Parent page; a nested page follows its parent's Space.</param>
/// <param name="SpaceId">Space for a root page (Editor level on that Space required); must be omitted with <paramref name="ParentId"/>.</param>
public record CreatePageRequest(string Title, Guid? ParentId = null, Guid? SpaceId = null);
