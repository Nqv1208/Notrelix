using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Policies;

public class WorkspacePolicy : AuditableEntity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public GuestAccessPolicy GuestPolicy { get; private set; } = null!;
    public ResourcePolicy ResourcePolicy { get; private set; } = null!;
    public SharingPolicy SharingPolicy { get; private set; } = null!;

    private WorkspacePolicy() : base() { }

    public static WorkspacePolicy Create(Guid workspaceId, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);

        var policy = new WorkspacePolicy
        {
            WorkspaceId = workspaceId,
            GuestPolicy = GuestAccessPolicy.Create(true),
            ResourcePolicy = ResourcePolicy.Create(false),
            SharingPolicy = SharingPolicy.Create(false, false)
        };

        policy.SetAuditOnCreate(createdBy, createdAt);
        return policy;
    }

    public void UpdatePolicy(
        GuestAccessPolicy? guestPolicy, 
        ResourcePolicy? resourcePolicy, 
        SharingPolicy? sharingPolicy, 
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        if (guestPolicy != null) GuestPolicy = guestPolicy;
        if (resourcePolicy != null) ResourcePolicy = resourcePolicy;
        if (sharingPolicy != null) SharingPolicy = sharingPolicy;

        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new WorkspacePolicyUpdatedEvent(WorkspaceId, updatedBy, updatedAt));
    }
}
