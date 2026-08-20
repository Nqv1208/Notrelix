namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// Generates one-time recovery codes and derives their non-recoverable
/// verifiers. Plaintext codes are returned exactly once at issuance time.
/// </summary>
public interface IMfaRecoveryCodeGenerator
{
    IReadOnlyList<string> Generate(int count);

    /// <summary>Deterministic verifier for a recovery code; never reversible.</summary>
    string Hash(string code);
}
