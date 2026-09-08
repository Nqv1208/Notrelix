namespace Notrelix.Infrastructure.Data.Integrations;

/// <summary>
/// M8 — technical physical-secret persistence: the encrypted blob behind an
/// opaque <c>SecretReference</c>. Infrastructure reliability state, not a
/// business model — the reference authority remains
/// <c>IntegrationSecretVersion</c> (Domain). Revocation is the compensation
/// path when a connect workflow fails after the secret was stored.
/// </summary>
public class IntegrationSecretBlob
{
    public Guid Id { get; private set; }
    public string EncryptedPayload { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public bool Revoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private IntegrationSecretBlob() { }

    public static IntegrationSecretBlob Create(string encryptedPayload, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(encryptedPayload))
            throw new ArgumentException("Encrypted payload is required.", nameof(encryptedPayload));

        return new IntegrationSecretBlob
        {
            Id = Guid.CreateVersion7(),
            EncryptedPayload = encryptedPayload,
            CreatedAt = createdAt
        };
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (Revoked) return;
        Revoked = true;
        RevokedAt = revokedAt;
    }
}
