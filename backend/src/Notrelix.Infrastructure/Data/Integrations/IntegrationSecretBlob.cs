namespace Notrelix.Infrastructure.Data.Integrations;

/// <summary>
/// M8 — technical physical-secret persistence: the encrypted blob behind an
/// opaque <c>SecretReference</c>. Infrastructure reliability state, not a
/// business model — the reference authority remains
/// <c>IntegrationSecretVersion</c> (Domain). The blob row and the connect
/// aggregates share one scoped context and one transaction fate, so a failed
/// workflow never leaves an orphan blob; revocation additionally marks a blob
/// whose secret is retired (e.g. the last binding revoked via CAL-CONN-001).
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
