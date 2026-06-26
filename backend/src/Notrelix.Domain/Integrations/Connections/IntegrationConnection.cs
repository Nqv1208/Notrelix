namespace Notrelix.Domain.Integrations.Connections;

public class IntegrationScope : Entity
{
    public Guid ConnectionId { get; private set; }
    public string Scope { get; private set; } = null!;

    private IntegrationScope() : base() { }

    public static IntegrationScope Create(Guid connectionId, string scope)
    {
        Guard.NotEmpty(connectionId);
        Guard.NotNullOrWhiteSpace(scope);

        return new IntegrationScope
        {
            ConnectionId = connectionId,
            Scope = scope.Trim()
        };
    }
}

public class IntegrationSecretVersion : Entity
{
    public Guid ConnectionId { get; private set; }
    public string Version { get; private set; } = null!;
    public SecretRef SecretReference { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private IntegrationSecretVersion() : base() { }

    public static IntegrationSecretVersion Create(Guid connectionId, string version, SecretRef secretRef, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(connectionId);
        Guard.NotNullOrWhiteSpace(version);
        Guard.NotNull(secretRef);

        return new IntegrationSecretVersion
        {
            ConnectionId = connectionId,
            Version = version.Trim(),
            SecretReference = secretRef,
            CreatedAt = createdAt
        };
    }
}

public class IntegrationConnection : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public IntegrationConnectionStatus Status { get; private set; }
    public string? ProviderAccountId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private readonly List<IntegrationScope> _scopes = new();
    public IReadOnlyCollection<IntegrationScope> Scopes => _scopes.AsReadOnly();

    private readonly List<IntegrationSecretVersion> _secretVersions = new();
    public IReadOnlyCollection<IntegrationSecretVersion> SecretVersions => _secretVersions.AsReadOnly();

    private IntegrationConnection() : base() { }

    public static IntegrationConnection Create(
        Guid workspaceId,
        IntegrationProvider provider,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? providerAccountId = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);

        if (expiresAt.HasValue && expiresAt.Value <= createdAt)
        {
            throw new DomainException("Expiration time must be in the future.");
        }

        var connection = new IntegrationConnection
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            Status = IntegrationConnectionStatus.Active,
            ProviderAccountId = providerAccountId,
            ExpiresAt = expiresAt
        };

        connection.SetAuditOnCreate(createdBy, createdAt);
        connection.AddDomainEvent(new IntegrationConnectionCreatedDomainEvent(workspaceId, connection.Id, provider, createdBy, createdAt));

        return connection;
    }

    public void Disconnect(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (Status == IntegrationConnectionStatus.Revoked) return;

        Status = IntegrationConnectionStatus.Revoked;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationConnectionRevokedDomainEvent(WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void Reconnect(string? providerAccountId, DateTimeOffset? expiresAt, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (expiresAt.HasValue && expiresAt.Value <= occurredAt)
        {
            throw new DomainException("Expiration time must be in the future.");
        }

        Status = IntegrationConnectionStatus.Active;
        ProviderAccountId = providerAccountId;
        ExpiresAt = expiresAt;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationConnectionReauthorizedDomainEvent(WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void MarkExpired(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (Status == IntegrationConnectionStatus.Expired) return;

        Status = IntegrationConnectionStatus.Expired;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationConnectionExpiredDomainEvent(WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void RotateSecret(string version, SecretRef secretRef, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(version);
        Guard.NotNull(secretRef);

        if (_secretVersions.Any(v => v.Version == version))
        {
            throw new DomainException($"Secret version '{version}' already exists for this connection.");
        }

        var newVersion = IntegrationSecretVersion.Create(Id, version, secretRef, occurredAt);
        _secretVersions.Add(newVersion);

        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationSecretRotatedDomainEvent(WorkspaceId, Id, version, updatedBy, occurredAt));
    }

    public void AddScope(string scope, Guid addedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(scope);
        if (_scopes.Any(s => s.Scope == scope)) return;

        _scopes.Add(IntegrationScope.Create(Id, scope));
        SetAuditOnUpdate(addedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationScopeAddedDomainEvent(WorkspaceId, Id, scope, addedBy, occurredAt));
    }

    public void RemoveScope(string scope, Guid removedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(scope);
        var scopeObj = _scopes.FirstOrDefault(s => s.Scope == scope);
        if (scopeObj == null) return;

        _scopes.Remove(scopeObj);
        SetAuditOnUpdate(removedBy, occurredAt);
        IncrementVersion();
        AddDomainEvent(new IntegrationScopeRemovedDomainEvent(WorkspaceId, Id, scope, removedBy, occurredAt));
    }
}
