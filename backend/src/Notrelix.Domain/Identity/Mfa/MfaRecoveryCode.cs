namespace Notrelix.Domain.Identity.Mfa;

public sealed class MfaRecoveryCode
{
    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTimeOffset? ConsumedAt { get; private set; }

    private MfaRecoveryCode() { }

    internal static MfaRecoveryCode Create(Guid batchId, string codeHash)
    {
        Guard.NotNullOrWhiteSpace(codeHash);
        return new MfaRecoveryCode
        {
            Id = Guid.CreateVersion7(),
            BatchId = batchId,
            CodeHash = codeHash,
            ConsumedAt = null
        };
    }

    internal void MarkConsumed(DateTimeOffset consumedAt)
    {
        ConsumedAt = consumedAt;
    }
}
