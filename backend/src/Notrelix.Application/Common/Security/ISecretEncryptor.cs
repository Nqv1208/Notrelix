namespace Notrelix.Application.Common.Security;

public interface ISecretEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string Protect(string plaintext, string purpose);
    string Unprotect(string ciphertext, string purpose);
}
