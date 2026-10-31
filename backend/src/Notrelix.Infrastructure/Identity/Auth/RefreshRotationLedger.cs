using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Infrastructure.Identity.Auth;

/// <summary>
/// Redis-backed rotation coordination. The claim is the first atomic increment of a key; the held
/// credential expires shortly after the grace window, and the database (rotation time) decides whether
/// the window is still open, so this store only has to carry the value. The held credential is stored
/// encrypted under a key derived from the credential that was replaced, which only its legitimate
/// presenter holds, so reading Redis yields no live credential.
/// </summary>
public sealed class RefreshRotationLedger : IRefreshRotationLedger
{
    private static readonly TimeSpan HoldTtl = SessionLifetimePolicy.RotationGrace + TimeSpan.FromSeconds(5);
    private static readonly byte[] KeyInfo = "notrelix.identity.refresh-successor.v1"u8.ToArray();
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IRedisCacheService _cache;

    public RefreshRotationLedger(IRedisCacheService cache)
    {
        _cache = cache;
    }

    public async Task<bool> TryClaimAsync(string presentedHash, CancellationToken ct) =>
        await _cache.IncrementAsync(ClaimKey(presentedHash), HoldTtl, ct) == 1;

    public Task ReleaseClaimAsync(string presentedHash, CancellationToken ct) =>
        _cache.RemoveAsync(ClaimKey(presentedHash), ct);

    private static string ClaimKey(string presentedHash) => $"identity:refresh:claim:{presentedHash}";

    public Task RecordSuccessorAsync(string presentedRefreshToken, string newRefreshToken, CancellationToken ct)
    {
        var presentedHash = RefreshTokenHash.Create(presentedRefreshToken).Hash;
        var plaintext = Encoding.UTF8.GetBytes(newRefreshToken);
        var sealedValue = new byte[NonceSize + TagSize + plaintext.Length];
        var nonce = sealedValue.AsSpan(0, NonceSize);
        var tag = sealedValue.AsSpan(NonceSize, TagSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(DeriveKey(presentedRefreshToken), TagSize);
        aes.Encrypt(nonce, plaintext, sealedValue.AsSpan(NonceSize + TagSize), tag, Encoding.UTF8.GetBytes(presentedHash));

        return _cache.SetAsync(SuccessorKey(presentedHash), Convert.ToBase64String(sealedValue), HoldTtl, ct);
    }

    public async Task<string?> RecallSuccessorAsync(string presentedRefreshToken, CancellationToken ct)
    {
        var presentedHash = RefreshTokenHash.Create(presentedRefreshToken).Hash;
        var stored = await _cache.GetAsync<string>(SuccessorKey(presentedHash), ct);
        if (stored is null) return null;

        try
        {
            var sealedValue = Convert.FromBase64String(stored);
            if (sealedValue.Length < NonceSize + TagSize) return null;

            var plaintext = new byte[sealedValue.Length - NonceSize - TagSize];
            using var aes = new AesGcm(DeriveKey(presentedRefreshToken), TagSize);
            aes.Decrypt(
                sealedValue.AsSpan(0, NonceSize), sealedValue.AsSpan(NonceSize + TagSize), sealedValue.AsSpan(NonceSize, TagSize),
                plaintext, Encoding.UTF8.GetBytes(presentedHash));

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            // Unreadable (tampered, written by another presenter, or malformed) answers as unknown.
            return null;
        }
    }

    private static string SuccessorKey(string presentedHash) => $"identity:refresh:successor:{presentedHash}";

    private static byte[] DeriveKey(string presentedRefreshToken) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(presentedRefreshToken), 32, info: KeyInfo);
}
