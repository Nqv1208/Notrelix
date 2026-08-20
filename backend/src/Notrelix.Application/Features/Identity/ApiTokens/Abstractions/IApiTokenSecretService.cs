namespace Notrelix.Application.Features.Identity.ApiTokens.Abstractions;

/// <summary>
/// Issues and digests opaque API token secrets. The raw secret is returned to the
/// caller exactly once at issuance; only the digest is persisted or queryable.
/// </summary>
public interface IApiTokenSecretService
{
    /// <summary>Creates a new raw secret together with its persisted digest.</summary>
    IssuedApiTokenSecret Generate();

    /// <summary>Computes the persisted digest of a presented raw secret.</summary>
    string Hash(string rawToken);
}

/// <summary>Raw API token secret (shown once at issuance) plus its one-way digest.</summary>
public sealed record IssuedApiTokenSecret(
    string RawToken,
    string TokenHash);