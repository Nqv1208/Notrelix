namespace Notrelix.Domain.Governance.Policies;

public class WorkspacePolicy : AuditableEntity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public GuestAccessPolicy GuestPolicy { get; private set; } = null!;
    public ResourcePolicy ResourcePolicy { get; private set; } = null!;
    public SharingPolicy SharingPolicy { get; private set; } = null!;

    private WorkspacePolicy() : base() { }

    public static WorkspacePolicy Create(Guid accountId, Guid workspaceId, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(accountId);

        var policy = new WorkspacePolicy
        {
            AccountId = accountId,
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
        RaiseDomainEvent(new WorkspacePolicyUpdatedEvent(AccountId, WorkspaceId, updatedBy, updatedAt));
    }
}
