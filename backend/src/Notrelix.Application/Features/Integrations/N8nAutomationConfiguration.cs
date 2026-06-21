using System.Text.Json;

namespace Notrelix.Application.Features.Integrations;

public static class N8nAutomationConfiguration
{
    public static bool TryGetWebhookPath(string configuration, out string webhookPath)
    {
        webhookPath = string.Empty;

        if (string.IsNullOrWhiteSpace(configuration))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            var root = document.RootElement;

            if (TryReadString(root, "webhookPath", out webhookPath) ||
                TryReadString(root, "webhook_path", out webhookPath))
            {
                webhookPath = webhookPath.Trim().TrimStart('/');
                return webhookPath.Length > 0;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
