using Notrelix.Application.Features.Documents.DTOs;
namespace Notrelix.Application.Features.Documents.Common;

internal static class DocumentDtoMapper
{
    public static PageDto ToPageDto(Page page) => ToPageDto(page, visibleSpaceIds: []);

    /// <param name="visibleSpaceIds">Spaces the caller can see; a placement in any other Space is not disclosed.</param>
    public static PageDto ToPageDto(Page page, IReadOnlyCollection<Guid> visibleSpaceIds) => new(
        page.Id,
        page.WorkspaceId,
        page.ParentId,
        page.Title,
        page.Icon,
        page.CoverImage,
        page.CreatedAt.DateTime,
        page.UpdatedAt?.DateTime,
        page.Version,
        page.SpaceId is { } spaceId && visibleSpaceIds.Contains(spaceId) ? spaceId : null
    );

    public static BlockDto ToBlockDto(Block block) => new(
        block.Id,
        block.PageId,
        block.ParentId,
        block.Type.ToString(),
        ToJson(block.Content?.Data.Value),
        ToJson(block.Properties?.Data.Value),
        block.Position.Value,
        (int)block.Version,
        block.CreatedAt.DateTime,
        block.UpdatedAt?.DateTime
    );

    private static System.Text.Json.JsonElement ToJson(string? json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json ?? "{}");
        return document.RootElement.Clone();
    }
}
