using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Infrastructure.Security.Hashing;
using Notrelix.Infrastructure.Security.Tokens;

namespace Notrelix.Infrastructure.Security.ApiTokens;

/// <summary>
/// API token secret factory (v4 §8.3). The raw secret is a versioned, URL-safe,
/// CSPRNG-backed opaque value shown exactly once at issuance; only its SHA-256
/// digest is persisted and used for lookup/verification.
/// </summary>
public sealed class ApiTokenSecretService : IApiTokenSecretService
{
    public const string ApiTokenPrefix = "ntk_v1.";
    /// <summary>Absolute bound on a presented token length, protecting the verifier from oversized input.</summary>
    public const int MaximumTokenLength = 512;

    private readonly RandomTokenGenerator _generator = new();
    private readonly TokenHasher _hasher = new();

    public IssuedApiTokenSecret Generate()
    {
        var rawToken = ApiTokenPrefix + _generator.Generate(byteLength: 32);
        return new IssuedApiTokenSecret(rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
        => _hasher.Hash(rawToken.Trim());
}