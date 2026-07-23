namespace Notrelix.Domain.SharedKernel;

/// <summary>
/// Generates a fractional index string that sorts lexicographically between two given indices.
/// Used for dynamic reorder operations where the number of positions is unbounded.
/// </summary>
public static class FractionalIndexMidpoint
{
    private const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const char MidChar = 'V';

    public static string Between(string low, string high)
    {
        if (string.CompareOrdinal(low, high) >= 0)
            throw new ArgumentException($"low ({low}) must be less than high ({high})");

        var maxLen = Math.Max(low.Length, high.Length);

        for (var i = 0; i < maxLen; i++)
        {
            var lo = i < low.Length ? low[i] : '\0';
            var hi = i < high.Length ? high[i] : '\0';

            if (lo == hi) continue;

            if (hi == '\0' || (lo != '\0' && hi - lo > 1))
            {
                var mid = lo == '\0' ? (char)('0' + 1) : (char)(lo + 1);
                return low[..i] + mid;
            }

            return low[..(i + 1)] + MidChar;
        }

        return low + MidChar;
    }
}
