using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;

namespace Notrelix.Infrastructure.Identity.Mfa;

/// <summary>
/// RFC 6238 TOTP implementation (HMAC-SHA1, 6 digits, 30s step) with
/// base32 secrets, at-rest protection via ASP.NET Core Data Protection,
/// and a ±1 step verification window.
/// </summary>
public sealed class MfaTotpService : IMfaTotpService
{
    private const string Purpose = "Notrelix.Mfa.Totp.Secret.v1";

    private readonly IDataProtector _protector;

    public MfaTotpService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string GenerateSecretKey()
    {
        var bytes = new byte[20];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    public string BuildOtpAuthUri(string base32Secret, string accountName, string issuer)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period={MfaPolicy.TotpTimeStepSeconds}";
    }

    public bool VerifyCode(string base32Secret, string code, DateTimeOffset now)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length != 6)
        {
            return false;
        }

        byte[] secret;
        try
        {
            secret = Base32Decode(base32Secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var counter = now.ToUnixTimeSeconds() / MfaPolicy.TotpTimeStepSeconds;

        var expected = GenerateCode(secret, counter);
        if (FixedTimeEquals(expected, normalized))
        {
            return true;
        }

        for (var offset = 1; offset <= MfaPolicy.TotpAllowedDriftSteps; offset++)
        {
            if (FixedTimeEquals(GenerateCode(secret, counter - offset), normalized) ||
                FixedTimeEquals(GenerateCode(secret, counter + offset), normalized))
            {
                return true;
            }
        }

        return false;
    }

    public string ProtectSecret(string base32Secret) => _protector.Protect(base32Secret);

    public string UnprotectSecret(string protectedSecret) => _protector.Unprotect(protectedSecret);

    private static string GenerateCode(byte[] secret, long counter)
    {
        var counterBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return new string(code.Where(char.IsDigit).ToArray());
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);

        var buffer = 0;
        var bits = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                sb.Append(alphabet[(buffer >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            sb.Append(alphabet[(buffer << (5 - bits)) & 0x1F]);
        }

        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var cleaned = new string(input
            .Where(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '2' and <= '7')
            .Select(char.ToUpperInvariant)
            .ToArray());

        var outputLength = cleaned.Length * 5 / 8;
        var output = new byte[outputLength];

        var buffer = 0;
        var bits = 0;
        var index = 0;

        foreach (var c in cleaned)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException($"Invalid base32 character '{c}'.");
            }

            buffer = (buffer << 5) | value;
            bits += 5;

            if (bits >= 8)
            {
                output[index++] = (byte)((buffer >> (bits - 8)) & 0xFF);
                bits -= 8;
            }
        }

        return output;
    }
}
