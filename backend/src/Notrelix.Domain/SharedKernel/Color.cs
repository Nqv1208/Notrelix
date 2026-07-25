using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Color : ValueObject
{
    private static readonly Regex HexColorRegex = new(
        @"^#(?:[0-9a-fA-F]{3}){1,2}$",
        RegexOptions.Compiled);

    public string Value { get; } = null!;

    private Color() { }
    private Color(string value)
    {
        Value = value;
    }

    public static Color Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        value = value.Trim().ToUpperInvariant();

        if (!HexColorRegex.IsMatch(value))
            throw new BusinessRuleException(CommonRuleCodes.SharedKernel_Color_InvalidFormat, $"'{value}' is not a valid hex color code.");

        if (value.Length == 4)
            value = $"#{value[1]}{value[1]}{value[2]}{value[2]}{value[3]}{value[3]}";

        return new Color(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
