using Notrelix.Domain.Integrations.Connections.Events;
using static Notrelix.Domain.Integrations.IntegrationRuleCodes;

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

public class IntegrationConnection : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public IntegrationConnectionStatus Status { get; private set; }
    public string? ProviderAccountId { get; private set; }
    public string? ErrorDetail { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    // Current secret state (replaces unbounded _secretVersions collection)
    public string? CurrentSecretVersion { get; private set; }
    public SecretRef? CurrentSecretRef { get; private set; }
    public DateTimeOffset? SecretRotatedAt { get; private set; }

    private readonly List<IntegrationScope> _scopes = new();
    public IReadOnlyCollection<IntegrationScope> Scopes => _scopes.AsReadOnly();

    private IntegrationConnection() : base() { }

    public static IntegrationConnection Create(
        Guid accountId,
        Guid workspaceId,
        IntegrationProvider provider,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? providerAccountId = null,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);

        if (expiresAt.HasValue && expiresAt.Value <= createdAt)
        {
            throw new BusinessRuleException(Integrations_Connection_ExpirationMustBeFuture, "Expiration time must be in the future.");
        }

        var connection = new IntegrationConnection
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Provider = provider,
            Status = IntegrationConnectionStatus.Active,
            ProviderAccountId = providerAccountId,
            ExpiresAt = expiresAt
        };

        connection.SetAuditOnCreate(createdBy, createdAt);
        connection.RaiseDomainEvent(new IntegrationConnectionCreatedDomainEvent(accountId, workspaceId, connection.Id, provider, createdBy, createdAt));

        return connection;
    }

    public void Disconnect(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (Status == IntegrationConnectionStatus.Revoked) return;

        Status = IntegrationConnectionStatus.Revoked;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionRevokedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void Reconnect(string? providerAccountId, DateTimeOffset? expiresAt, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (expiresAt.HasValue && expiresAt.Value <= occurredAt)
        {
            throw new BusinessRuleException(Integrations_Connection_ExpirationMustBeFuture, "Expiration time must be in the future.");
        }

        // Normalize provider account ID
        var normalizedProviderAccountId = providerAccountId?.Trim();

        // No-op detection: already Active with same values and no pending error
        if (Status == IntegrationConnectionStatus.Active &&
            ProviderAccountId == normalizedProviderAccountId &&
            ExpiresAt == expiresAt &&
            ErrorDetail is null)
        {
            return;
        }

        Status = IntegrationConnectionStatus.Active;
        ProviderAccountId = normalizedProviderAccountId;
        ExpiresAt = expiresAt;
        ErrorDetail = null;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionReauthorizedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void MarkExpired(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (Status == IntegrationConnectionStatus.Expired) return;

        Status = IntegrationConnectionStatus.Expired;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionExpiredDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void MarkError(string error, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(error);

        var trimmedError = error.Trim();

        // No-op detection: already Error with same detail
        if (Status == IntegrationConnectionStatus.Error && ErrorDetail == trimmedError)
        {
            return;
        }

        ErrorDetail = trimmedError;
        Status = IntegrationConnectionStatus.Error;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionErrorRecordedDomainEvent(AccountId, WorkspaceId, Id, trimmedError, updatedBy, occurredAt));
    }

    public void RotateSecret(string version, SecretRef secretRef, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(version);
        Guard.NotNull(secretRef);

        var trimmedVersion = version.Trim();

        // No-op detection: same version and secret ref
        if (CurrentSecretVersion == trimmedVersion && CurrentSecretRef == secretRef)
        {
            return;
        }

        CurrentSecretVersion = trimmedVersion;
        CurrentSecretRef = secretRef;
        SecretRotatedAt = occurredAt;

        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationSecretRotatedDomainEvent(AccountId, WorkspaceId, Id, trimmedVersion, updatedBy, occurredAt));
    }

    public void AddScope(string scope, Guid addedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(scope);
        var trimmedScope = scope.Trim();
        if (_scopes.Any(s => s.Scope == trimmedScope)) return;

        _scopes.Add(IntegrationScope.Create(Id, trimmedScope));
        SetAuditOnUpdate(addedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationScopeAddedDomainEvent(AccountId, WorkspaceId, Id, trimmedScope, addedBy, occurredAt));
    }

    public void RemoveScope(string scope, Guid removedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(scope);
        var trimmedScope = scope.Trim();
        var scopeObj = _scopes.FirstOrDefault(s => s.Scope == trimmedScope);
        if (scopeObj == null) return;

        _scopes.Remove(scopeObj);
        SetAuditOnUpdate(removedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationScopeRemovedDomainEvent(AccountId, WorkspaceId, Id, trimmedScope, removedBy, occurredAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = IntegrationConnectionStatus.Revoked;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = IntegrationConnectionStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new IntegrationConnectionRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
