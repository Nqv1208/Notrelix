using Notrelix.Application.Features.Document.DTOs;
namespace Notrelix.Application.Features.Document.Common;

internal static class DocumentDtoMapper
{
    public static PageDto ToPageDto(Page page) => new(
        page.Id,
        page.WorkspaceId,
        page.ParentId,
        page.Title,
        page.IconType,
        page.IconValue,
        page.CoverUrl,
        page.Position,
        page.Depth,
        page.IsTemplate,
        page.IsArchived,
        page.PublishedAt,
        page.Deadline,
        page.CreatedAt,
        page.UpdatedAt
    );

    public static BlockDto ToBlockDto(Block block) => new(
        block.Id,
        block.PageId,
        block.ParentBlockId,
        block.Type,
        block.Properties,
        block.Properties,
        block.Position,
        block.Version,
        block.CreatedByUserId,
        block.CreatedAt,
        block.UpdatedAt
    );
}
