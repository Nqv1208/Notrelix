using Notrelix.Domain.Common;

namespace Notrelix.Domain.Documents.ResourceLinks;

public class ResourceLink : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public ResourceRef Target { get; private set; } = null!;
    public LinkType Type { get; private set; }

    private ResourceLink() : base() { }

    public static ResourceLink Create(Guid workspaceId, ResourceRef source, ResourceRef target, LinkType type, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotNull(target);

        var link = new ResourceLink
        {
            WorkspaceId = workspaceId,
            Source = source,
            Target = target,
            Type = type
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        return link;
    }
}
