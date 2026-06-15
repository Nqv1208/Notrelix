using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public enum SsoProviderStatus
{
    Draft,
    Enabled,
    Disabled,
    Deleted
}

public class SsoProvider : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public SsoProviderType ProviderType { get; private set; }
    public string Name { get; private set; } = null!;
    public SsoProviderStatus Status { get; private set; } = SsoProviderStatus.Draft;
    public SsoProviderConfiguration? Configuration { get; private set; }

    private SsoProvider() : base() { }

    public static SsoProvider Create(
        Guid workspaceId,
        SsoProviderType type,
        string name,
        Guid createdBy,
        DateTimeOffset createdAt,
        SsoProviderConfiguration? configuration = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var provider = new SsoProvider
        {
            WorkspaceId = workspaceId,
            ProviderType = type,
            Name = name.Trim(),
            Configuration = configuration,
            Status = SsoProviderStatus.Draft
        };

        provider.SetAuditOnCreate(createdBy, createdAt);
        provider.AddDomainEvent(new SsoProviderCreatedDomainEvent(workspaceId, provider.Id, provider.Name, createdBy, createdAt));
        return provider;
    }

    public void Enable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = SsoProviderStatus.Enabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new SsoProviderEnabledDomainEvent(WorkspaceId, Id, Name, updatedBy, updatedAt));
        IncrementVersion();
    }

    public void Disable(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == SsoProviderStatus.Disabled) return;
        Status = SsoProviderStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new SsoProviderDisabledDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new SsoProviderSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new SsoProviderRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
