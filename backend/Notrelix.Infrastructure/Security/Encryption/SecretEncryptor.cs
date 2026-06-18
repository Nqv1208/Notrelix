namespace Notrelix.Infrastructure.Security.Encryption;

/// <summary>
/// Skeleton symmetric secret encryptor (v4 §8.3). Real implementation encrypts
/// integration credentials at rest (stored as SecretRef/encrypted reference,
/// never plaintext) using a managed key. Not yet wired.
/// </summary>
public sealed class SecretEncryptor
{
    // TODO(v4 §8.3): Encrypt(plaintext)/Decrypt(ciphertext) using DataProtection
    // or an envelope-encryption KMS. Integration credentials must never be plaintext.
}
