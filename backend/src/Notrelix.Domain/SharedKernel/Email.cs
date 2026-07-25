using System.Text.RegularExpressions;

namespace Notrelix.Domain.SharedKernel;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; } = null!;

    private Email() { }
    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);

        value = value.Trim().ToLowerInvariant();

        if (value.Length > 254)
            throw new BusinessRuleException(CommonRuleCodes.SharedKernel_Email_InvalidFormat, "Email address exceeds maximum length of 254 characters.");

        if (!EmailRegex.IsMatch(value))
            throw new BusinessRuleException(CommonRuleCodes.SharedKernel_Email_InvalidFormat, $"'{value}' is not a valid email address.");

        return new Email(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
