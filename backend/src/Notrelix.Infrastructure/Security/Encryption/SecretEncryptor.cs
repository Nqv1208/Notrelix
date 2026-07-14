namespace Notrelix.Infrastructure.Security.Encryption;

public sealed class SecretEncryptor : ISecretEncryptor
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public SecretEncryptor(IDataProtectionProvider dataProtection)
    {
        _dataProtectionProvider = dataProtection;
    }

    public string Encrypt(string plaintext)
        => Protect(plaintext, "Notrelix.Integrations.Credentials.v1");

    public string Decrypt(string ciphertext)
        => Unprotect(ciphertext, "Notrelix.Integrations.Credentials.v1");

    public string Protect(string plaintext, string purpose)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var protector = _dataProtectionProvider.CreateProtector(purpose);
        return Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(plaintext)));
    }

    public string Unprotect(string ciphertext, string purpose)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var protector = _dataProtectionProvider.CreateProtector(purpose);
        return Encoding.UTF8.GetString(protector.Unprotect(Convert.FromBase64String(ciphertext)));
    }
}
