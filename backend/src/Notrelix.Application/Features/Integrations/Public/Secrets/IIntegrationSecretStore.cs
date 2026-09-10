namespace Notrelix.Application.Features.Integrations.Public.Secrets;

/// <summary>
/// M8 freeze — the Integrations-owned secret persistence boundary. Physical
/// secret storage is external to the aggregates: the port stores a secret and
/// returns an opaque reference that <c>IntegrationSecretVersion</c> records as
/// its <c>SecretReference</c> authority. The port never exposes secret
/// material to the Domain and can be swapped (DataProtection blob store now,
/// cloud KMS/Vault later) without touching the aggregate contract.
/// </summary>
public interface IIntegrationSecretStore
{
    /// <summary>
    /// Persists the secret under an opaque reference.
    /// </summary>
    Task<string> StoreAsync(string secret, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the physical secret behind the reference. Revoking an unknown
    /// or already-revoked reference is a no-op.
    /// </summary>
    Task RevokeAsync(string secretReference, CancellationToken cancellationToken);
}
