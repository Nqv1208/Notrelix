using System.Security.Cryptography;
using System.Text;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Credentials;

public sealed class TokenHash : ValueObject
{
    public string Hash { get; }

    private TokenHash(string hash)
    {
        Hash = hash;
    }

    public static TokenHash Create(string rawToken)
    {
        Guard.NotNullOrWhiteSpace(rawToken);

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        var hash = Convert.ToBase64String(bytes);

        return new TokenHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
