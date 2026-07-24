// Ported from rocicorp/fractional-indexing v4.0.0.
// Default classic key format only.
// Custom digits/intDigits intentionally unsupported.
// Original project license: CC0-1.0.

namespace Notrelix.Domain.SharedKernel.Ordering;

/// <summary>
/// Generates fractional ordering keys using the canonical fractional-indexing
/// algorithm. Keys are base-62 strings compared via ordinal (lexicographic) ordering.
/// </summary>
public static class FractionalIndexGenerator
{
    private const string Digits =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const string IntegerHeads =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private static readonly int DigitsLength = Digits.Length; // 62

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a key strictly between <paramref name="lower"/> and
    /// <paramref name="upper"/>. Either bound may be null (unbounded).
    /// </summary>
    public static FractionalIndex GenerateKeyBetween(
        FractionalIndex? lower,
        FractionalIndex? upper)
    {
        ValidateBounds(lower, upper);

        var value = GenerateKeyBetweenCore(lower?.Value, upper?.Value);

        ValidateGeneratedKey(value, lower, upper);

        return FractionalIndex.Create(value);
    }

    /// <summary>
    /// Generates <paramref name="count"/> keys strictly between
    /// <paramref name="lower"/> and <paramref name="upper"/>, evenly distributed.
    /// </summary>
    public static IReadOnlyList<FractionalIndex> GenerateNKeysBetween(
        FractionalIndex? lower,
        FractionalIndex? upper,
        int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        ValidateBounds(lower, upper);

        var values = GenerateNKeysBetweenCore(lower?.Value, upper?.Value, count);

        var results = new FractionalIndex[values.Length];
        for (var i = 0; i < values.Length; i++)
            results[i] = FractionalIndex.Create(values[i]);

        ValidateGeneratedKeys(results, lower, upper, count);

        return results;
    }

    /// <summary>
    /// Validates that a string conforms to the canonical fractional-indexing
    /// key grammar. Throws if invalid.
    /// </summary>
    internal static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Order key cannot be null or empty.", nameof(key));

        // The null character is explicitly forbidden.
        if (key[0] == '\0')
            throw new ArgumentException($"Invalid order key: '{key}'.", nameof(key));

        // Validate the integer part (head + digits).
        GetIntegerPart(key);

        // Validate all characters are in the digit set.
        for (var i = 0; i < key.Length; i++)
        {
            if (Digits.IndexOf(key[i]) < 0)
                throw new ArgumentException(
                    $"Invalid order key '{key}': character '{key[i]}' is not in the digit set.",
                    nameof(key));
        }
    }

    // ── Core algorithm ────────────────────────────────────────────────────

    private static string GenerateKeyBetweenCore(string? a, string? b)
    {
        if (a is not null && b is not null && StringComparer.Ordinal.Compare(a, b) >= 0)
            throw new ArgumentException($"lower ({a}) must be less than upper ({b}).");

        if (a is null && b is null)
            return "a0";

        if (a is null)
            return GenerateBefore(b!);

        if (b is null)
            return GenerateAfter(a);

        return GenerateBetween(a, b);
    }

    private static string[] GenerateNKeysBetweenCore(string? a, string? b, int n)
    {
        if (n == 1)
            return [GenerateKeyBetweenCore(a, b)];

        if (b is null)
        {
            var c = GenerateKeyBetweenCore(a, b);
            var result = new string[n];
            result[0] = c;
            for (var i = 1; i < n; i++)
                result[i] = GenerateKeyBetweenCore(result[i - 1], b);
            return result;
        }

        if (a is null)
        {
            var c = GenerateKeyBetweenCore(a, b);
            var result = new string[n];
            result[^1] = c;
            for (var i = n - 2; i >= 0; i--)
                result[i] = GenerateKeyBetweenCore(a, result[i + 1]);
            return result;
        }

        var mid = (n - 1) / 2;
        var left = GenerateNKeysBetweenCore(a, b, mid + 1);
        var right = GenerateNKeysBetweenCore(left[^1], b, n - mid - 1);
        return [.. left, .. right];
    }

    private static string GenerateBefore(string b)
    {
        var integerPart = GetIntegerPart(b);
        var head = integerPart[0];

        // Try to decrement the integer part.
        var decremented = DecrementInteger(integerPart);
        if (decremented is not null)
            return decremented;

        // Cannot decrement further — use the fractional part.
        // Prepend a digit before b's fractional part.
        var fractionalPart = b[integerPart.Length..];
        return integerPart + Midpoint(null, fractionalPart);
    }

    private static string GenerateAfter(string a)
    {
        var integerPart = GetIntegerPart(a);

        // Try to increment the integer part.
        var incremented = IncrementInteger(integerPart);
        if (incremented is not null)
            return incremented;

        // Cannot increment further — use the fractional part.
        var fractionalPart = a[integerPart.Length..];
        return integerPart + Midpoint(fractionalPart, null);
    }

    private static string GenerateBetween(string a, string b)
    {
        var intA = GetIntegerPart(a);
        var intB = GetIntegerPart(b);

        if (intA != intB)
        {
            // Different integer parts — try to find an integer between them.
            var incremented = IncrementInteger(intA);
            if (incremented is not null && StringComparer.Ordinal.Compare(incremented, intB) < 0)
                return incremented;

            // No integer between them — use a's integer + fractional midpoint.
            var fracA = a[intA.Length..];
            return intA + Midpoint(fracA, null);
        }

        // Same integer part — find midpoint of fractional parts.
        var fracPartA = a[intA.Length..];
        var fracPartB = b[intB.Length..];
        return intA + Midpoint(fracPartA, fracPartB);
    }

    // ── Integer part helpers ──────────────────────────────────────────────

    private static int GetIntegerLength(char head)
    {
        if (head is >= 'a' and <= 'z')
            return head - 'a' + 2;

        if (head is >= 'A' and <= 'Z')
            return 'Z' - head + 2;

        throw new ArgumentException($"Invalid order key head: '{head}'.");
    }

    private static string GetIntegerPart(string key)
    {
        if (key.Length == 0)
            throw new ArgumentException("Order key cannot be empty.");

        var integerPartLength = GetIntegerLength(key[0]);
        if (key.Length < integerPartLength)
            throw new ArgumentException($"Invalid order key: '{key}'.");

        return key[..integerPartLength];
    }

    private static string? IncrementInteger(string x)
    {
        var head = x[0];
        var digits = x[1..];

        var newDigits = IncrementDigits(digits);
        if (newDigits is not null)
            return head + newDigits;

        // Carry: move to the next head.
        if (head is >= 'a' and < 'z')
        {
            var newHead = (char)(head + 1);
            return newHead + new string('0', GetIntegerLength(newHead) - 1);
        }

        if (head is >= 'A' and > 'Z')
        {
            // This branch is unreachable for valid heads, but guards against corruption.
            return null;
        }

        if (head == 'Z')
        {
            // Z is the smallest positive head. Incrementing wraps to 'a' (zero).
            return "a" + new string('0', GetIntegerLength('a') - 1);
        }

        if (head is >= 'A' and < 'Z')
        {
            // A-Y: decrement head (toward A = larger integer length).
            var newHead = (char)(head - 1);
            return newHead + new string('0', GetIntegerLength(newHead) - 1);
        }

        // head == 'z': overflow, cannot increment.
        return null;
    }

    private static string? DecrementInteger(string x)
    {
        var head = x[0];
        var digits = x[1..];

        var newDigits = DecrementDigits(digits);
        if (newDigits is not null)
            return head + newDigits;

        // Borrow: move to the previous head.
        if (head is > 'a' and <= 'z')
        {
            var newHead = (char)(head - 1);
            return newHead + new string('z', GetIntegerLength(newHead) - 1);
        }

        if (head == 'a')
        {
            // 'a' is the zero head. Decrementing wraps to 'Z' (negative one).
            return "Z" + new string('z', GetIntegerLength('Z') - 1);
        }

        if (head is >= 'A' and < 'Z')
        {
            var newHead = (char)(head + 1);
            return newHead + new string('z', GetIntegerLength(newHead) - 1);
        }

        // head == 'A': underflow, cannot decrement.
        return null;
    }

    private static string? IncrementDigits(string digits)
    {
        var chars = digits.ToCharArray();
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            var idx = Digits.IndexOf(chars[i]);
            if (idx < DigitsLength - 1)
            {
                chars[i] = Digits[idx + 1];
                return new string(chars);
            }
            chars[i] = Digits[0]; // carry
        }
        return null; // all digits were max → overflow
    }

    private static string? DecrementDigits(string digits)
    {
        var chars = digits.ToCharArray();
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            var idx = Digits.IndexOf(chars[i]);
            if (idx > 0)
            {
                chars[i] = Digits[idx - 1];
                return new string(chars);
            }
            chars[i] = Digits[DigitsLength - 1]; // borrow
        }
        return null; // all digits were min → underflow
    }

    // ── Midpoint ──────────────────────────────────────────────────────────

    /// <summary>
    /// Computes a digit string strictly between <paramref name="a"/> and
    /// <paramref name="b"/>. Either may be null (unbounded).
    /// </summary>
    private static string Midpoint(string? a, string? b)
    {
        if (a is not null && b is not null && StringComparer.Ordinal.Compare(a, b) >= 0)
            throw new ArgumentException($"lower ({a}) must be less than upper ({b}).");

        // Pad a with implicit zeros; b with implicit max-digits.
        var maxLen = Math.Max(a?.Length ?? 0, b?.Length ?? 0);

        for (var i = 0; i < maxLen; i++)
        {
            var digitA = i < (a?.Length ?? 0) ? Digits.IndexOf(a![i]) : 0;
            var digitB = i < (b?.Length ?? 0) ? Digits.IndexOf(b![i]) : DigitsLength;

            if (digitA == digitB)
                continue;

            if (digitB - digitA > 1)
            {
                // There is room between these digits.
                var mid = (digitA + digitB) / 2;
                var prefix = (a?[..i]) ?? "";
                return prefix + Digits[mid];
            }

            // digitB - digitA == 1: no room. Take digitA and recurse into
            // the next position with a's suffix as lower bound.
            var prefix2 = (a?[..(i + 1)]) ?? Digits[digitA].ToString();
            var suffixA = (a is not null && i + 1 < a.Length) ? a[(i + 1)..] : null;
            return prefix2 + Midpoint(suffixA, null);
        }

        // a is a prefix of b (or both empty up to maxLen).
        // Append a midpoint digit after a.
        var baseStr = a ?? "";
        return baseStr + Digits[DigitsLength / 2]; // 'V'
    }

    // ── Validation helpers ────────────────────────────────────────────────

    private static void ValidateBounds(FractionalIndex? lower, FractionalIndex? upper)
    {
        if (lower is not null && upper is not null && lower.CompareTo(upper) >= 0)
            throw new ArgumentException(
                $"Lower bound ({lower.Value}) must be less than upper bound ({upper.Value}).");
    }

    private static void ValidateGeneratedKey(
        string value,
        FractionalIndex? lower,
        FractionalIndex? upper)
    {
        ValidateKey(value);

        if (lower is not null && StringComparer.Ordinal.Compare(lower.Value, value) >= 0)
            throw new InvalidOperationException(
                $"Generated key '{value}' must be greater than lower bound '{lower.Value}'.");

        if (upper is not null && StringComparer.Ordinal.Compare(value, upper.Value) >= 0)
            throw new InvalidOperationException(
                $"Generated key '{value}' must be less than upper bound '{upper.Value}'.");
    }

    private static void ValidateGeneratedKeys(
        IReadOnlyList<FractionalIndex> keys,
        FractionalIndex? lower,
        FractionalIndex? upper,
        int expectedCount)
    {
        if (keys.Count != expectedCount)
            throw new InvalidOperationException("Generated key count is invalid.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < keys.Count; i++)
        {
            if (!seen.Add(keys[i].Value))
                throw new InvalidOperationException("Generated keys must be unique.");

            if (i > 0 && keys[i - 1].CompareTo(keys[i]) >= 0)
                throw new InvalidOperationException("Generated keys must be strictly ordered.");
        }

        if (keys.Count > 0)
        {
            ValidateGeneratedKey(keys[0].Value, lower, upper);
            ValidateGeneratedKey(keys[^1].Value, lower, upper);
        }
    }
}
