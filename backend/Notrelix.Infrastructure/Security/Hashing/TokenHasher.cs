using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Infrastructure.Security.Hashing;

/// <summary>
/// One-way hasher for opaque tokens (v4 §8.3). Tokens/secrets are never stored
/// in plaintext — only their hash. Pure/inert. Constant-time comparison provided.
/// </summary>
public sealed class TokenHasher
{
    public string Hash(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken))
            throw new ArgumentException("Token cannot be empty.", nameof(rawToken));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string rawToken, string storedHash)
    {
        if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(storedHash))
            return false;

        var computed = Encoding.UTF8.GetBytes(Hash(rawToken));
        var stored = Encoding.UTF8.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(computed, stored);
    }
}
