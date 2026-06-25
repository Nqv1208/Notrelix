using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Color : ValueObject
{
    private static readonly Regex HexColorRegex = new(
        @"^#(?:[0-9a-fA-F]{3}){1,2}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Color() { }
    private Color(string value)
    {
        Value = value;
    }

    public static Color Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        value = value.Trim().ToUpperInvariant();

        Guard.Assert(HexColorRegex.IsMatch(value), $"'{value}' is not a valid hex color code.");

        return new Color(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
