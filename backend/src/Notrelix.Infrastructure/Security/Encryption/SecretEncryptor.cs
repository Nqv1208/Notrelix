using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Notrelix.Infrastructure.Security.Encryption;

public interface ISecretEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public sealed class SecretEncryptor : ISecretEncryptor
{
    private readonly IDataProtector _protector;

    public SecretEncryptor(IDataProtectionProvider dataProtection)
    {
        _protector = dataProtection.CreateProtector("Notrelix.Integrations.Credentials.v1");
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(plaintext)));
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        return Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(ciphertext)));
    }
}
