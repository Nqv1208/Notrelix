namespace Notrelix.Application.Features.Chat.Links.DTOs;

/// <summary>
/// What a link to a product resource looks like to the caller (UC-CHT-G05, CHT-023). <c>Title</c> is present only when the caller can view the
/// resource now; otherwise <c>Accessible</c> is false and the link stays generic, the same answer whether the resource is hidden, trashed or
/// missing. <c>Kind</c> is the ResourceRef kind (work-management.board-item or documents.page) and <c>ResourceId</c> its id.
/// </summary>
public sealed record LinkPreviewDto(string Kind, Guid ResourceId, bool Accessible, string? Title);
