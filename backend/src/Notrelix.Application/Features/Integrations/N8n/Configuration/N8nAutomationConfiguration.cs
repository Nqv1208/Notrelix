using System.Text.Json;

namespace Notrelix.Application.Features.Integrations.N8n.Configuration;

/// <summary>
/// Reads the n8n webhook path from a Webhook action configuration. Only a path
/// inside the <see cref="N8nWebhookPath"/> allow-list is returned.
/// </summary>
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

            if (root.ValueKind == JsonValueKind.Object &&
                TryReadString(root, "webhookPath", out var candidate) &&
                N8nWebhookPath.IsValid(candidate))
            {
                webhookPath = candidate;
                return true;
            }

            // AUT-028: the stored path is not trusted even though creation
            // validates it (rules created before the allow-list, or edited out
            // of band, fail closed here). It is never trimmed or normalised
            // into validity.
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
