namespace Notrelix.Infrastructure.Messaging.Options;

public sealed class MessagingEndpointOptions
{
    public string Transport { get; set; } = "InMemory";

    public string EndpointPrefix { get; set; } = "notrelix";
}

public static class MessagingEndpointNames
{
    private const string StableProductionPrefix = "notrelix";

    public static string CalendarWebhookProcessingRequested(
        string? transport,
        string? endpointPrefix)
    {
        var prefix = string.Equals(transport, "RabbitMQ", StringComparison.OrdinalIgnoreCase)
            ? StableProductionPrefix
            : string.IsNullOrWhiteSpace(endpointPrefix)
                ? StableProductionPrefix
                : endpointPrefix.Trim();

        return $"{prefix}-integrations-calendar-webhook-processing-requested-v1";
    }
}
