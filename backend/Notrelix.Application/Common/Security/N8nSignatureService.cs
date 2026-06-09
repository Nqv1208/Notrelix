using System.Security.Cryptography;
using System.Text;
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.Application.Common.Security;

public sealed class N8nSignatureService : IN8nSignatureService
{
    public string CreateSignature(string payload, long timestamp, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));

        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public bool VerifySignature(
        string payload,
        long timestamp,
        string signature,
        string secret,
        TimeSpan tolerance,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var current = now ?? DateTimeOffset.UtcNow;
        var sentAt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (current - sentAt > tolerance || sentAt - current > tolerance)
        {
            return false;
        }

        var expected = CreateSignature(payload, timestamp, secret);
        return FixedTimeEquals(expected, signature);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
