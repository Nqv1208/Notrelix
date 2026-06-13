using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class ResourceRef : ValueObject
{
    public string ResourceType { get; }
    public Guid ResourceId { get; }

    private ResourceRef() { }    private ResourceRef(string resourceType, Guid resourceId)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public static ResourceRef Create(string resourceType, Guid resourceId)
    {
        Guard.NotNullOrWhiteSpace(resourceType);
        Guard.Assert(resourceId != Guid.Empty, "ResourceId cannot be empty.");

        return new ResourceRef(resourceType.Trim(), resourceId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ResourceType;
        yield return ResourceId;
    }

    public override string ToString() => $"{ResourceType}:{ResourceId}";
}
