namespace Notrelix.Infrastructure.Data.Projections.Search;

public sealed class SearchDocumentRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Content { get; private set; }
    public string[] Tags { get; private set; } = [];
    public string MetadataJson { get; private set; } = null!;
    public NpgsqlTsVector? SearchVector { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private SearchDocumentRecord() { }

    public static SearchDocumentRecord Create(
        Guid id,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string title,
        string? content,
        string[] tags,
        string metadataJson,
        DateTimeOffset createdAt)
    {
        return new SearchDocumentRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Title = title,
            Content = content,
            Tags = tags,
            MetadataJson = metadataJson,
            CreatedAt = createdAt,
        };
    }
}
