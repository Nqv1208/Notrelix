using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Domain.Governance.ShareLinks;

public sealed class ShareLinkTokenHash : ValueObject
{
    public string Hash { get; private set; } = null!;

    private ShareLinkTokenHash() { }
    private ShareLinkTokenHash(string hash)
    {
        Hash = hash;
    }

    public static ShareLinkTokenHash Create(string rawToken)
    {
        Guard.NotNullOrWhiteSpace(rawToken);

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        var hash = Convert.ToBase64String(bytes);

        return new ShareLinkTokenHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
