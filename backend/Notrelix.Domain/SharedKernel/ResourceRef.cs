using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class ResourceRef : ValueObject
{
    public string ResourceType { get; }
    public Guid ResourceId { get; }
    public Guid? WorkspaceId { get; }

    private ResourceRef() { }
    private ResourceRef(string resourceType, Guid resourceId, Guid? workspaceId)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        WorkspaceId = workspaceId;
    }

    public static ResourceRef Create(string resourceType, Guid resourceId, Guid? workspaceId = null)
    {
        Guard.NotNullOrWhiteSpace(resourceType);
        Guard.Assert(resourceId != Guid.Empty, "ResourceId cannot be empty.");

        return new ResourceRef(resourceType.Trim(), resourceId, workspaceId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ResourceType;
        yield return ResourceId;
        yield return WorkspaceId;
    }

    public override string ToString() => $"{ResourceType}:{ResourceId}";
}
