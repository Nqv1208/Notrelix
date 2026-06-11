using Notrelix.Domain.Common;

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

    public static IntegrationSecretVersion Create(Guid connectionId, string version, SecretRef secretRef)
    {
        Guard.NotEmpty(connectionId);
        Guard.NotNullOrWhiteSpace(version);
        Guard.NotNull(secretRef);

        return new IntegrationSecretVersion
        {
            ConnectionId = connectionId,
            Version = version.Trim(),
            SecretReference = secretRef,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

public class IntegrationConnection : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public IntegrationConnectionStatus Status { get; private set; }
    public string? ProviderAccountId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private readonly List<IntegrationScope> _scopes = new();
    public IReadOnlyCollection<IntegrationScope> Scopes => _scopes.AsReadOnly();

    private IntegrationConnection() : base() { }

    public static IntegrationConnection Create(
        Guid workspaceId, 
        IntegrationProvider provider, 
        Guid createdBy,
        string? providerAccountId = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);

        var connection = new IntegrationConnection
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            Status = IntegrationConnectionStatus.Active,
            ProviderAccountId = providerAccountId,
            ExpiresAt = expiresAt
        };

        connection.SetAuditOnCreate(createdBy);
        connection.AddDomainEvent(new IntegrationConnectionCreatedEvent(workspaceId, connection.Id, provider, createdBy));

        return connection;
    }

    public void AddScope(string scope)
    {
        if (_scopes.Any(s => s.Scope == scope)) return;
        _scopes.Add(IntegrationScope.Create(Id, scope));
    }
}
