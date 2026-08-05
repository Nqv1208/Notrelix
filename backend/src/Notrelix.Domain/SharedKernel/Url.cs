namespace Notrelix.Domain.SharedKernel;

public sealed class Url : ValueObject
{
    public string Value { get; } = null!;

    private Url() { }
    private Url(string value)
    {
        Value = value;
    }

    public static Url Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        value = value.Trim();

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            throw new BusinessRuleException(SharedKernelRuleCodes.SharedKernel_Url_InvalidFormat, $"'{value}' is not a valid HTTP or HTTPS URL.");

        // Normalize: scheme and host to lowercase for deterministic equality.
        var builder = new UriBuilder(uriResult)
        {
            Scheme = uriResult.Scheme.ToLowerInvariant(),
            Host = uriResult.Host.ToLowerInvariant()
        };

        return new Url(builder.Uri.ToString());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
