using Notrelix.Application.Features.Identity.Mfa.Abstractions;

namespace Notrelix.Infrastructure.Identity.Mfa;

/// <summary>
/// Cryptographically random one-time recovery codes in display form
/// "XXXX-XXXX-XXXX-XXXX-XXXX", verified through irreversible SHA-256
/// verifiers so stored codes can never be recovered or leaked.
/// </summary>
public sealed class MfaRecoveryCodeGenerator : IMfaRecoveryCodeGenerator
{
    public IReadOnlyList<string> Generate(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Recovery code count must be positive.");
        }

        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            codes.Add(GenerateSingle());
        }

        return codes;
    }

    public string Hash(string code)
    {
        var canonical = Canonicalize(code);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateSingle()
    {
        var bytes = new byte[15];
        RandomNumberGenerator.Fill(bytes);

        var raw = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', 'A')
            .Replace('/', 'B')
            .ToUpperInvariant();

        var groups = new List<string>(5);
        for (var i = 0; i < raw.Length; i += 4)
        {
            groups.Add(raw.Substring(i, Math.Min(4, raw.Length - i)));
        }

        return string.Join("-", groups);
    }

    private static string Canonicalize(string code)
    {
        return new string(code
            .Where(c => char.IsLetterOrDigit(c))
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
