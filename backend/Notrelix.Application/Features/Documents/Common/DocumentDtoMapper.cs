using Notrelix.Application.Features.Document.DTOs;
namespace Notrelix.Application.Features.Document.Common;

internal static class DocumentDtoMapper
{
    public static PageDto ToPageDto(Page page) => new(
        page.Id,
        page.WorkspaceId,
        page.ParentId,
        page.Title,
        page.Icon,
        page.CoverImage,
        page.CreatedAt.DateTime,
        page.UpdatedAt?.DateTime
    );

    public static BlockDto ToBlockDto(Block block) => new(
        block.Id,
        block.PageId,
        block.ParentId,
        block.Type.ToString(),
        block.Content.ToString(),
        block.Properties.ToString(),
        block.Position.Value,
        (int)block.Version,
        block.CreatedAt.DateTime,
        block.UpdatedAt?.DateTime
    );
}
