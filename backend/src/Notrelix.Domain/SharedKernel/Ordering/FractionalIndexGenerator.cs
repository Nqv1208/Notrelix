// Ported from rocicorp/fractional-indexing v4.0.0.
// Default classic key format only (BASE_62 digits, A-Z/a-z heads).
// Custom digits/intDigits intentionally unsupported.
// Original project license: CC0-1.0.
// Commit: f1193a7 (v4.0.0 tag).

namespace Notrelix.Domain.SharedKernel.Ordering;

/// <summary>
/// Generates fractional ordering keys using the canonical fractional-indexing
/// algorithm. Keys are base-62 strings compared via ordinal (lexicographic) ordering.
/// </summary>
public static class FractionalIndexGenerator
{
    private const string Digits =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const string IntDigits =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private static readonly int DigitsLength = Digits.Length;
    private static readonly int IntDigitsLength = IntDigits.Length;

    private static readonly int[] DigitLookup = BuildLookup(Digits);
    private static readonly int[] IntLookup = BuildLookup(IntDigits);

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a key strictly between <paramref name="lower"/> and
    /// <paramref name="upper"/>. Either bound may be null (unbounded).
    /// Throws if both bounds are provided and lower >= upper.
    /// </summary>
    public static FractionalIndex GenerateKeyBetween(
        FractionalIndex? lower,
        FractionalIndex? upper)
    {
        var a = lower?.Value;
        var b = upper?.Value;

        ValidateBounds(a, b);

        var value = GenerateKeyBetweenCore(a, b);

        return FractionalIndex.Create(value);
    }

    /// <summary>
    /// Generates <paramref name="count"/> keys strictly between
    /// <paramref name="lower"/> and <paramref name="upper"/>, evenly distributed.
    /// Throws if both bounds are provided and lower >= upper.
    /// </summary>
    public static IReadOnlyList<FractionalIndex> GenerateNKeysBetween(
        FractionalIndex? lower,
        FractionalIndex? upper,
        int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        var a = lower?.Value;
        var b = upper?.Value;

        ValidateBounds(a, b);

        var values = GenerateNKeysBetweenCore(a, b, count);

        var results = new FractionalIndex[values.Length];
        for (var i = 0; i < values.Length; i++)
            results[i] = FractionalIndex.Create(values[i]);

        return results;
    }

    private static void ValidateBounds(string? a, string? b)
    {
        if (a != null && b != null && StringComparer.Ordinal.Compare(a, b) >= 0)
            throw new ArgumentException(
                $"Lower bound ({a}) must be less than upper bound ({b}).");
    }

    /// <summary>
    /// Validates that a string conforms to the canonical fractional-indexing
    /// key grammar. Throws if invalid.
    /// </summary>
    internal static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Order key cannot be null or empty.", nameof(key));

        ValidateOrderKey(key);
    }

    // ── Core algorithm (port of upstream generateKeyBetween) ──────────────

    private static string GenerateKeyBetweenCore(string? a, string? b)
    {
        if (a == null && b == null)
        {
            var head = IntDigits[IntDigitsLength / 2];
            return new string([head, Digits[0]]);
        }

        if (a == null)
        {
            var ib = GetIntegerPart(b!);
            var fb = b![ib.Length..];
            if (IsSmallestInteger(ib))
                return ib + Midpoint("", fb);
            if (StringComparer.Ordinal.Compare(ib, b) < 0)
                return ib;
            var res = DecrementInteger(ib);
            if (res == null)
                throw new ArgumentException("Cannot decrement any more.");
            return res;
        }

        if (b == null)
        {
            var ia = GetIntegerPart(a);
            var fa = a[ia.Length..];
            var i = IncrementInteger(ia);
            return i == null ? ia + Midpoint(fa, null) : i;
        }

        var ia2 = GetIntegerPart(a);
        var fa2 = a[ia2.Length..];
        var ib2 = GetIntegerPart(b);
        var fb2 = b[ib2.Length..];
        if (ia2 == ib2)
            return ia2 + Midpoint(fa2, fb2);

        var inc = IncrementInteger(ia2);
        if (inc == null)
            throw new ArgumentException("Cannot increment any more.");
        if (inc != null && StringComparer.Ordinal.Compare(inc, b) < 0)
            return inc;

        return ia2 + Midpoint(fa2, null);
    }

    private static string[] GenerateNKeysBetweenCore(string? a, string? b, int n)
    {
        if (n == 0)
            return [];

        if (n == 1)
            return [GenerateKeyBetweenCore(a, b)];

        if (b == null)
        {
            var c = GenerateKeyBetweenCore(a, b);
            var result = new string[n];
            result[0] = c;
            for (var i = 1; i < n; i++)
            {
                c = GenerateKeyBetweenCore(c, b);
                result[i] = c;
            }
            return result;
        }

        if (a == null)
        {
            var c = GenerateKeyBetweenCore(a, b);
            var result = new string[n];
            result[^1] = c;
            for (var i = n - 2; i >= 0; i--)
            {
                result[i] = GenerateKeyBetweenCore(a, result[i + 1]);
            }
            return result;
        }

        var mid = n / 2;
        var left = GenerateNKeysBetweenCore(a, b, mid);
        var c2 = GenerateKeyBetweenCore(left[^1], b);
        var right = GenerateNKeysBetweenCore(c2, b, n - mid - 1);
        return [.. left, c2, .. right];
    }

    // ── Midpoint (port of upstream midpoint) ──────────────────────────────

    private static string Midpoint(string? a, string? b)
    {
        var zero = Digits[0];

        if (b != null && a != null && StringComparer.Ordinal.Compare(a, b) >= 0)
            throw new ArgumentException($"lower ({a}) must be less than upper ({b}).");

        if (a != null && a.Length > 0 && a[^1] == zero)
            throw new ArgumentException("Trailing zero in key.", nameof(a));

        if (b != null && b.Length > 0 && b[^1] == zero)
            throw new ArgumentException("Trailing zero in key.", nameof(b));

        if (b != null)
        {
            var n = 0;
            while (true)
            {
                var aDigit = n < (a?.Length ?? 0) ? a![n] : zero;
                var bDigit = b[n];
                if (aDigit != bDigit)
                    break;
                n++;
            }

            if (n > 0)
            {
                var prefix = b[..n];
                var aSlice = n < (a?.Length ?? 0) ? a![n..] : null;
                var bSlice = b[n..];
                return prefix + Midpoint(aSlice, bSlice);
            }
        }

        var digitA = a != null && a.Length > 0 ? GetDigitIndex(a[0], DigitLookup) : 0;
        var digitB = b != null && b.Length > 0 ? GetDigitIndex(b[0], DigitLookup) : DigitsLength;

        if (digitB - digitA > 1)
        {
            var midDigit = (int)Math.Round(0.5 * (digitA + digitB), MidpointRounding.AwayFromZero);
            return Digits[midDigit].ToString();
        }

        if (b != null && b.Length > 1)
            return b[..1];

        return Digits[digitA] + Midpoint(a?.Length > 0 ? a[1..] : null, null);
    }

    // ── Integer part helpers ──────────────────────────────────────────────

    private static int GetIntegerLength(char head)
    {
        var i = GetDigitIndex(head, IntLookup);
        if (IntDigits[i] == head)
        {
            var half = IntDigitsLength / 2;
            return i < half ? half - i + 1 : i - half + 2;
        }

        throw new ArgumentException($"Invalid order key head: '{head}'.");
    }

    private static string GetIntegerPart(string key)
    {
        if (key.Length == 0)
            throw new ArgumentException("Order key cannot be empty.");

        var length = GetIntegerLength(key[0]);
        if (length > key.Length)
            throw new ArgumentException($"Invalid order key: '{key}'.");

        return key[..length];
    }

    private static string? IncrementInteger(string x)
    {
        var head = x[0];
        var trailing = "";

        for (var i = x.Length - 1; i >= 1; i--)
        {
            var d = GetDigitIndex(x[i], DigitLookup) + 1;
            if (d == DigitsLength)
            {
                trailing = Digits[0] + trailing;
            }
            else
            {
                return x[..i] + Digits[d] + trailing;
            }
        }

        var headIndex = GetDigitIndex(head, IntLookup);
        if (headIndex == IntDigitsLength - 1)
            return null;

        var h = IntDigits[headIndex + 1];
        var lengthDelta = GetIntegerLength(h) - GetIntegerLength(head);

        return lengthDelta > 0
            ? h + trailing + Digits[0]
            : lengthDelta < 0
                ? h + trailing[1..]
                : h + trailing;
    }

    private static string? DecrementInteger(string x)
    {
        var head = x[0];
        var last = Digits[DigitsLength - 1];
        var trailing = "";

        for (var i = x.Length - 1; i >= 1; i--)
        {
            var d = GetDigitIndex(x[i], DigitLookup) - 1;
            if (d == -1)
            {
                trailing = last + trailing;
            }
            else
            {
                return x[..i] + Digits[d] + trailing;
            }
        }

        var headIndex = GetDigitIndex(head, IntLookup);
        if (headIndex == 0)
            return null;

        var h = IntDigits[headIndex - 1];
        var lengthDelta = GetIntegerLength(h) - GetIntegerLength(head);

        return lengthDelta > 0
            ? h + trailing + last
            : lengthDelta < 0
                ? h + trailing[1..]
                : h + trailing;
    }

    // ── Validation (port of upstream validateOrderKey + helpers) ──────────

    private static bool IsSmallestInteger(string key)
    {
        var head = IntDigits[0];
        var zero = Digits[0];
        var half = IntDigitsLength / 2;
        var expected = head + new string(zero, half);
        return key == expected;
    }

    private static void ValidateOrderKey(string key)
    {
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (c >= DigitLookup.Length || DigitLookup[c] < 0)
                throw new ArgumentException(
                    $"Invalid order key character '{c}' in key '{key}'.", nameof(key));
        }

        if (IsSmallestInteger(key))
            throw new ArgumentException($"Invalid order key: '{key}'.", nameof(key));

        var head = GetIntegerLength(key[0]);
        if (head > key.Length)
            throw new ArgumentException($"Invalid order key: '{key}'.", nameof(key));

        var f = key[head..];

        if (f.Length > 0 && f[^1] == Digits[0])
            throw new ArgumentException($"Invalid order key: '{key}'.", nameof(key));
    }

    private static int[] BuildLookup(string alphabet)
    {
        var lookup = Enumerable.Repeat(-1, 256).ToArray();
        for (var i = 0; i < alphabet.Length; i++)
            lookup[alphabet[i]] = i;
        return lookup;
    }

    private static int GetDigitIndex(char character, int[] lookup)
    {
        if (character >= lookup.Length || lookup[character] < 0)
            throw new ArgumentException(
                $"Invalid fractional-index character '{character}'.");

        return lookup[character];
    }
}
