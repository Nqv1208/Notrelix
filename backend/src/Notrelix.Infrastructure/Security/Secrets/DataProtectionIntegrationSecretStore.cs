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

    public async Task<string> StoreAsync(string secret, CancellationToken cancellationToken)
    {
        var blob = IntegrationSecretBlob.Create(
            _encryptor.Encrypt(secret),
            _clock.UtcNow);
        _context.IntegrationSecretBlobs.Add(blob);
        await _context.SaveChangesAsync(cancellationToken);
        return blob.Id.ToString();
    }

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
