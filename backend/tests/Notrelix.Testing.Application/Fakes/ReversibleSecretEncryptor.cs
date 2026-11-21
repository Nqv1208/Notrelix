using Notrelix.Application.Common.Security;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>A secret store whose "ciphertext" is obviously not the plaintext but can be read back with the same purpose.</summary>
public sealed class ReversibleSecretEncryptor : ISecretEncryptor
{
    public string Encrypt(string plaintext) => Protect(plaintext, "default");

    public string Decrypt(string ciphertext) => Unprotect(ciphertext, "default");

    public string Protect(string plaintext, string purpose) =>
        $"enc:{purpose}:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));

    public string Unprotect(string ciphertext, string purpose)
    {
        var prefix = $"enc:{purpose}:";
        if (!ciphertext.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Wrong purpose or not a protected value.");
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext[prefix.Length..]));
    }
}
