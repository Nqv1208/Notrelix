namespace Notrelix.Domain.Integrations.Webhooks;

public sealed class WebhookSecretHash : ValueObject
{
    public string Hash { get; } = null!;

    private WebhookSecretHash() { }
    private WebhookSecretHash(string hash)
    {
        Hash = hash;
    }

    public static WebhookSecretHash Create(string hash)
    {
        Guard.NotNullOrWhiteSpace(hash);
        return new WebhookSecretHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
