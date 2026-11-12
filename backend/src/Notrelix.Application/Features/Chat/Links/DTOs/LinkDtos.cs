namespace Notrelix.Application.Features.Chat.Links.DTOs;

/// <summary>
/// What a link to a product resource looks like to the caller (UC-CHT-G05, CHT-023). <c>Title</c> is present only when the caller can view the
/// resource now; otherwise <c>Accessible</c> is false and the link stays generic, the same answer whether the resource is hidden, trashed or
/// missing. <c>Kind</c> is the ResourceRef kind (work-management.board-item or documents.page) and <c>ResourceId</c> its id.
/// </summary>
public sealed record LinkPreviewDto(string Kind, Guid ResourceId, bool Accessible, string? Title);

/// <summary>
/// The Board, Page or Team a channel is linked to, as one viewer sees it (UC-CHT-H10, CHT-023). <c>Title</c> is present only when the viewer
/// can view the resource now; otherwise <c>Accessible</c> is false and the link stays generic, the same whether the resource is hidden,
/// trashed or gone. <c>Kind</c> is the ResourceRef kind (work-management.board, documents.page or governance.team).
/// </summary>
public sealed record ChannelLinkDto(string Kind, Guid ResourceId, bool Accessible, string? Title);

/// <summary>A channel linked to a resource that the caller can see: enough to offer a Discuss entry point (UC-CHT-H10).</summary>
public sealed record LinkedChannelDto(Guid Id, string Name, string Kind, bool IsMember, bool IsArchived);
