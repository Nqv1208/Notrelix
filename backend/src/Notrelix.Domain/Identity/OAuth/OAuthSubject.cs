using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public sealed class OAuthSubject : ValueObject
{
    public string Value { get; }

    private OAuthSubject() { }    private OAuthSubject(string value)
    {
        Value = value;
    }

    public static OAuthSubject Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new OAuthSubject(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
