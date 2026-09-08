using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Integrations;
using Notrelix.Application.Features.Integrations.Public.Secrets;

namespace Notrelix.Infrastructure.Security.Secrets;

/// <summary>
/// DataProtection-backed physical secret store. The secret is encrypted with
/// the application key ring and persisted as a blob row; the row id becomes
/// the opaque SecretReference handed to <c>IntegrationSecretVersion</c>.
/// The key ring must be durable in production (persisted keys), otherwise
/// restarts cannot decrypt stored secrets.
/// </summary>
public sealed class DataProtectionIntegrationSecretStore : IIntegrationSecretStore
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretEncryptor _encryptor;
    private readonly IDateTimeProvider _clock;

    public DataProtectionIntegrationSecretStore(
        ApplicationDbContext context,
        ISecretEncryptor encryptor,
        IDateTimeProvider clock)
    {
        _context = context;
        _encryptor = encryptor;
        _clock = clock;
    }

    /// <summary>
    /// Stages the encrypted blob on the SAME scoped context as the workflow
    /// aggregates — the DataSession transaction commits blob + aggregates as
    /// one fate (atomicity instead of compensation). The opaque reference is
    /// assigned on staging so the IntegrationSecretVersion can record it.
    /// </summary>
    public Task<string> StoreAsync(string secret, CancellationToken cancellationToken)
    {
        var blob = IntegrationSecretBlob.Create(
            _encryptor.Encrypt(secret),
            _clock.UtcNow);
        _context.IntegrationSecretBlobs.Add(blob);
        return Task.FromResult(blob.Id.ToString());
    }

    /// <summary>
    /// Commits the revocation of a physical secret (post-commit cleanup path —
    /// e.g. revoking a previous version's blob after a failed rotation).
    /// Unknown/already-revoked references are no-ops.
    /// </summary>
    public async Task RevokeAsync(string secretReference, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(secretReference, out var blobId))
        {
            return;
        }

        var blob = await _context.IntegrationSecretBlobs
            .FirstOrDefaultAsync(b => b.Id == blobId && !b.Revoked, cancellationToken);
        if (blob is null)
        {
            return;
        }

        blob.Revoke(_clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
