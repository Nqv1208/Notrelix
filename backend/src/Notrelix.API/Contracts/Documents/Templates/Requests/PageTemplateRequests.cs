namespace Notrelix.API.Contracts.Documents.Templates.Requests;

public record CreatePageTemplateRequest(Guid PageId, string Name, string? Description = null);

/// <param name="ExpectedVersion">The template version the manager read; a stale one is 412.</param>
public record UpdatePageTemplateRequest(string Name, string? Description, long ExpectedVersion);

public record PageTemplateVersionRequest(long ExpectedVersion);

/// <param name="Title">The new page's title; the template's title when left out.</param>
/// <param name="ParentId">Parent page; a nested page follows its parent's Space.</param>
/// <param name="SpaceId">Space for a root page; must be omitted with <paramref name="ParentId"/>.</param>
public record CreatePageFromTemplateRequest(string? Title = null, Guid? ParentId = null, Guid? SpaceId = null);
