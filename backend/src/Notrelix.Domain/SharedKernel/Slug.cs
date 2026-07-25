using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Slug : ValueObject
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public string Value { get; } = null!;

    private Slug() { }
    private Slug(string value)
    {
        Value = value;
    }

    public static Slug Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);

        value = value.Trim().ToLowerInvariant();

        if (!SlugRegex.IsMatch(value))
            throw new BusinessRuleException(CommonRuleCodes.SharedKernel_Slug_InvalidFormat, $"'{value}' is not a valid slug. Only lowercase letters, numbers, and hyphens are allowed.");

        return new Slug(value);
    }

    public static Slug GenerateFromName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);

        // Unicode normalization: FormD decomposes characters, then remove
        // combining marks (accents, diacritics) to get ASCII-compatible base.
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var value = sb.ToString().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-");
        value = value.Trim('-');

        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException(CommonRuleCodes.SharedKernel_Slug_InvalidFormat, "Name does not contain any characters that can form a valid slug.");

        return Create(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
