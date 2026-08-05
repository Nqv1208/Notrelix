namespace Notrelix.Domain.SharedKernel;

public sealed class ResourceRef : ValueObject
{
    public ResourceKind Kind { get; }
    public Guid ResourceId { get; }
    public Guid? WorkspaceId { get; }

    private ResourceRef() { }

    private ResourceRef(ResourceKind kind, Guid resourceId, Guid? workspaceId)
    {
        Kind = kind;
        ResourceId = resourceId;
        WorkspaceId = workspaceId;
    }

    public static ResourceRef Create(ResourceKind kind, Guid resourceId, Guid? workspaceId = null)
    {
        Guard.NotEmpty(resourceId);
        return new ResourceRef(kind, resourceId, workspaceId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return ResourceId;
        yield return WorkspaceId;
    }

    public void EnsureSameWorkspace(Guid workspaceId)
    {
        if (WorkspaceId.HasValue && WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{WorkspaceId.Value}'.");
    }

    public override string ToString() => $"{Kind}:{ResourceId}";
}
