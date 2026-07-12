using Microsoft.AspNetCore.WebUtilities;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using System.Text.RegularExpressions;

namespace Notrelix.Infrastructure.Security.Tokens;

public sealed class OneTimeTokenService : IOneTimeTokenService
{
    private const int TokenSizeInBytes = 32;
    public const int CurrentHashVersion = 1;
    private const int MaximumTokenLength = 256;
    private static readonly Regex TokenFormat = new(
        "^v(?<version>[1-9][0-9]*)\\.(?<secret>[A-Za-z0-9_-]+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IssuedOneTimeToken Generate(TokenPurpose purpose)
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        var rawToken = $"v{CurrentHashVersion}.{WebEncoders.Base64UrlEncode(bytes)}";

        return new IssuedOneTimeToken(
            rawToken,
            ComputeHash(rawToken, purpose, CurrentHashVersion),
            CurrentHashVersion);
    }

    public ParsedOneTimeToken ParseAndHash(
        string presentedToken,
        TokenPurpose expectedPurpose)
    {
        if (string.IsNullOrWhiteSpace(presentedToken))
            throw new InvalidOneTimeTokenException();

        var normalized = presentedToken.Trim();
        if (normalized.Length > MaximumTokenLength)
            throw new InvalidOneTimeTokenException();

        var match = TokenFormat.Match(normalized);
        if (!match.Success
            || !int.TryParse(match.Groups["version"].Value, out var hashVersion)
            || hashVersion != CurrentHashVersion)
        {
            throw new InvalidOneTimeTokenException();
        }

        try
        {
            var secret = WebEncoders.Base64UrlDecode(match.Groups["secret"].Value);
            if (secret.Length == 0)
                throw new InvalidOneTimeTokenException();
        }
        catch (FormatException)
        {
            throw new InvalidOneTimeTokenException();
        }

        return new ParsedOneTimeToken(
            ComputeHash(normalized, expectedPurpose, hashVersion),
            hashVersion);
    }

    private static string ComputeHash(
        string rawToken,
        TokenPurpose purpose,
        int hashVersion)
    {
        var input = $"v{hashVersion}:{(int)purpose}:{rawToken}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
