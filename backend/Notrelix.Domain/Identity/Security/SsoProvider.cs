using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public enum SsoProviderType
{
    SAML,
    OIDC
}

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
    public string? Domain { get; private set; }
    public string MetadataJson { get; private set; } = "{}";

    private SsoProvider() : base() { }

    public static SsoProvider Create(
        Guid workspaceId,
        SsoProviderType type,
        string name,
        string? domain,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? metadataJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var provider = new SsoProvider
        {
            WorkspaceId = workspaceId,
            ProviderType = type,
            Name = name.Trim(),
            Domain = domain?.Trim().ToLowerInvariant(),
            MetadataJson = metadataJson ?? "{}",
            Status = SsoProviderStatus.Draft
        };

        provider.SetAuditOnCreate(createdBy, createdAt);
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
        Status = SsoProviderStatus.Disabled;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }
}
