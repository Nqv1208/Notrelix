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

    public static PageVersionDto ToVersionDto(Notrelix.Domain.Documents.Versions.DocumentVersion version) => new(
        version.Id,
        version.PageId,
        version.VersionNumber,
        KindName(version.Kind),
        version.ChangeSummary,
        version.CreatedBy,
        version.CreatedAt.DateTime
    );

    public static string KindName(Notrelix.Domain.Documents.Versions.DocumentVersionKind kind) => kind switch
    {
        Notrelix.Domain.Documents.Versions.DocumentVersionKind.Named => "named",
        Notrelix.Domain.Documents.Versions.DocumentVersionKind.Restore => "restore",
        _ => "automatic"
    };

    public static PageVersionBlockDto ToVersionBlockDto(Notrelix.Application.Features.Documents.Versions.Services.SnapshotBlock block) => new(
        block.Id,
        block.ParentId,
        block.Type.ToString(),
        ToJson(block.Content),
        ToJson(block.Properties),
        block.Position
    );

    internal static System.Text.Json.JsonElement ToJson(string? json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json ?? "{}");
        return document.RootElement.Clone();
    }
}
