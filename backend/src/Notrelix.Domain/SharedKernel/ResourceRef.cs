namespace Notrelix.Domain.SharedKernel;

public sealed class ResourceRef : ValueObject
{
    public ResourceType ResourceType { get; }
    public Guid ResourceId { get; }
    public Guid? WorkspaceId { get; }

    private ResourceRef() { }
    private ResourceRef(ResourceType resourceType, Guid resourceId, Guid? workspaceId)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        WorkspaceId = workspaceId;
    }

    public static ResourceRef Create(ResourceType resourceType, Guid resourceId, Guid? workspaceId = null)
    {
        Guard.NotEmpty(resourceId);
        return new ResourceRef(resourceType, resourceId, workspaceId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ResourceType;
        yield return ResourceId;
        yield return WorkspaceId;
    }

    public void EnsureSameWorkspace(Guid workspaceId)
    {
        if (WorkspaceId.HasValue && WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(BusinessRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{WorkspaceId.Value}'.");
    }

    public override string ToString() => $"{ResourceType}:{ResourceId}";
}
