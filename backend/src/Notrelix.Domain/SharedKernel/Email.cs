using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email() { }
    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);

        value = value.Trim().ToLowerInvariant();

        Guard.Assert(EmailRegex.IsMatch(value), $"'{value}' is not a valid email address.");

        return new Email(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
