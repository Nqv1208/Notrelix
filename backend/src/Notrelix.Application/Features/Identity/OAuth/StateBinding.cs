using System.Security.Cryptography;
using System.Text;

namespace Notrelix.Application.Features.Identity.OAuth;

/// <summary>
/// Ties a sign-in state to the browser that started it. The start hands the browser a random value in a cookie
/// and keeps only its SHA-256 beside the state; the callback must present the same value.
/// </summary>
public static class StateBinding
{
    public static string NewValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string binding) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(binding)));

    /// <summary>Constant-time check of a presented binding against the stored hash; a missing binding or hash never matches.</summary>
    public static bool Matches(string? presented, string? storedHash) =>
        !string.IsNullOrEmpty(presented)
        && !string.IsNullOrEmpty(storedHash)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(presented)), Encoding.ASCII.GetBytes(storedHash));
}
