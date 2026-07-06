namespace Notrelix.Domain.Billing.Plans;

public sealed class FeatureCode : ValueObject
{
    public string Code { get; private set; } = null!;

    private FeatureCode() { }
    private FeatureCode(string code)
    {
        Code = code;
    }

    public static FeatureCode Create(string code)
    {
        Guard.NotNullOrWhiteSpace(code);
        return new FeatureCode(code.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
