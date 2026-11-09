using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

/// <summary>
/// AUT-029: rule configuration never stores a reusable provider secret. Provider secrets live in
/// Integrations behind a connection reference, so a trigger or action configuration that carries a
/// secret-like property name (for example <c>apiKey</c>, <c>password</c>, <c>accessToken</c>,
/// <c>authorization</c>) or an inline bearer credential value is rejected at validation time.
/// Names are compared case-insensitively with '-', '_', '.' and spaces ignored, at every depth.
/// Configuration that is not valid JSON is left to the existing per-type validators to report.
/// </summary>
public static class AutomationSecretFieldGuard
{
    private static readonly string[] SecretNameMarkers =
    [
        "password",
        "passwd",
        "secret",
        "token",
        "apikey",
        "authorization",
        "bearer",
        "privatekey",
    ];

    public static void Ensure(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            return;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(configuration);
        }
        catch (JsonException)
        {
            return;
        }

        using (document)
        {
            var offending = FindSecretLikeName(document.RootElement);
            if (offending is not null)
                throw new BusinessRuleException(
                    AutomationRuleCodes.Automation_Configuration_SecretFieldNotAllowed,
                    $"Automation configuration must not contain a secret-like field or value ({offending}). Connect the provider in Integrations and reference the connection instead.");
        }
    }

    internal static bool IsSecretLikeName(string name)
    {
        var normalized = Normalize(name);
        foreach (var marker in SecretNameMarkers)
        {
            if (normalized.Contains(marker, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string? FindSecretLikeName(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (IsSecretLikeName(property.Name))
                        return property.Name;

                    var nested = FindSecretLikeName(property.Value);
                    if (nested is not null)
                        return nested;
                }

                return null;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindSecretLikeName(item);
                    if (nested is not null)
                        return nested;
                }

                return null;

            case JsonValueKind.String:
                // An inline credential in a value, such as "Bearer abc.def", is a secret even under a neutral name.
                var text = element.GetString();
                return text is not null && text.TrimStart().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? "a Bearer credential"
                    : null;

            default:
                return null;
        }
    }

    private static string Normalize(string name)
    {
        var buffer = new char[name.Length];
        var length = 0;
        foreach (var c in name)
        {
            if (c is '-' or '_' or '.' or ' ')
                continue;
            buffer[length++] = char.ToLowerInvariant(c);
        }

        return new string(buffer, 0, length);
    }
}
