namespace Notrelix.Infrastructure.Security.Tokens;

/// <summary>
/// Cryptographically secure opaque token generator. Pure/inert — used for
/// refresh tokens, share-link secrets, API tokens, and similar secrets. No
/// persistence here; see <c>backend/docs/architecture/security-tenancy-authorization.md</c>.
/// </summary>
public sealed class RandomTokenGenerator
{
    /// <summary>Generates a URL-safe base64 token from <paramref name="byteLength"/> random bytes.</summary>
    public string Generate(int byteLength = 32)
    {
        if (byteLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(byteLength));

        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
