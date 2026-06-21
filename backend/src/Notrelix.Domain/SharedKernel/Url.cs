using System.Text.RegularExpressions;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.SharedKernel;

public sealed class Url : ValueObject
{
    public string Value { get; }

    private Url() { }    private Url(string value)
    {
        Value = value;
    }

    public static Url Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        value = value.Trim();

        Guard.Assert(Uri.TryCreate(value, UriKind.Absolute, out var uriResult) 
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps),
            $"'{value}' is not a valid HTTP or HTTPS URL.");

        return new Url(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
