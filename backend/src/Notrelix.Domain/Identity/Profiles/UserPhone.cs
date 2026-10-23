namespace Notrelix.Domain.Identity.Profiles;

/// <summary>
/// The account phone number rule. A phone is stored in normalized E.164 form: a leading
/// <c>+</c>, then 8 to 15 digits with a non-zero first digit (E.164 allows 15 digits at most;
/// numbers shorter than 8 are not dialable internationally). A person may type it with spaces,
/// hyphens, dots or parentheses as separators, which are removed before the check; any other
/// character is rejected. A blank value means "no phone" and normalizes to null.
/// </summary>
public static class UserPhone
{
    public const int MinDigits = 8;
    public const int MaxDigits = 15;

    /// <summary>Returns the normalized E.164 value, or null for a blank input; throws on an invalid number.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var candidate = input.Trim();
        if (!TryNormalize(candidate, out var normalized))
        {
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_Profile_InvalidPhone,
                "A phone number must be in international format: '+' followed by 8 to 15 digits.");
        }

        return normalized;
    }

    public static bool TryNormalize(string candidate, out string normalized)
    {
        normalized = string.Empty;
        if (candidate.Length is 0 or > 32 || candidate[0] != '+') return false;

        var digits = new System.Text.StringBuilder(MaxDigits);
        foreach (var c in candidate.AsSpan(1))
        {
            if (char.IsAsciiDigit(c)) digits.Append(c);
            else if (c is ' ' or '-' or '.' or '(' or ')') continue;
            else return false;
        }

        if (digits.Length is < MinDigits or > MaxDigits || digits[0] == '0') return false;

        normalized = "+" + digits;
        return true;
    }
}
