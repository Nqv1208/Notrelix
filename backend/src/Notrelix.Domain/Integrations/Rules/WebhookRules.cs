namespace Notrelix.Domain.Integrations.Rules;

public static class WebhookRules
{
    public static void EnsureMaxRetries(int maxRetries, int maxAllowed = 10)
    {
        if (maxRetries < 0 || maxRetries > maxAllowed)
            throw new BusinessRuleException($"Max retries must be between 0 and {maxAllowed}.");
    }

    public static void EnsureUrlIsValid(Url url)
    {
        Guard.NotNull(url);
    }
}
