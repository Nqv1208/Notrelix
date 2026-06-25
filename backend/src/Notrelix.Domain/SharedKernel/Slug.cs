using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Slug : ValueObject
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Slug() { }
    private Slug(string value)
    {
        Value = value;
    }

    public static Slug Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);

        value = value.Trim().ToLowerInvariant();

        Guard.Assert(SlugRegex.IsMatch(value), $"'{value}' is not a valid slug. Only lowercase letters, numbers, and hyphens are allowed.");

        return new Slug(value);
    }

    public static Slug GenerateFromName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        var value = name.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-");
        return new Slug(value.Trim('-'));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
