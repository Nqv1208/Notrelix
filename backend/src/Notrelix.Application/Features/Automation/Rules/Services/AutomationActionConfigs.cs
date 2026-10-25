using System.Text.Json;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>
/// The typed configuration of each runtime action, parsed in one place so rule authoring (save
/// validation) and execution read the same target. A malformed configuration parses to false.
/// </summary>
public static class AutomationActionConfigs
{
    public static class Types
    {
        public const string MoveItem = "MoveItem";
        public const string ChangeStatus = "ChangeStatus";
        public const string AssignPerson = "AssignPerson";
        public const string UpdateField = "UpdateField";
        public const string NotifyMember = "NotifyMember";
        public const string SendEmail = "SendEmail";
        public const string Webhook = "Webhook";
    }

    /// <summary>The actions a rule can run today; any other type has no executor and is not accepted.</summary>
    public static bool HasExecutor(string? type) =>
        type is Types.MoveItem or Types.ChangeStatus or Types.AssignPerson or Types.UpdateField
            or Types.NotifyMember or Types.SendEmail or Types.Webhook;

    /// <summary>Actions that act on the item the trigger fact was about (everything but a Webhook).</summary>
    public static bool ActsOnItem(string? type) => HasExecutor(type) && type != Types.Webhook;

    public static bool TryParseChangeStatus(string? configuration, out Guid optionId) =>
        TryGuid(configuration, "optionId", out optionId);

    public static bool TryParseAssign(string? configuration, out Guid userId) =>
        TryGuid(configuration, "userId", out userId);

    public static bool TryParseUpdateField(string? configuration, out Guid fieldId, out string valueJson)
    {
        valueJson = string.Empty;
        if (!TryGuid(configuration, "fieldId", out fieldId)) return false;
        using var document = JsonDocument.Parse(configuration!);
        if (!document.RootElement.TryGetProperty("value", out var value)) return false;
        valueJson = value.GetRawText();
        return true;
    }

    public static bool TryParseNotify(string? configuration, out Guid userId, out string? message)
    {
        message = null;
        if (!TryGuid(configuration, "userId", out userId)) return false;
        message = Text(configuration, "message");
        return true;
    }

    public static bool TryParseEmail(string? configuration, out Guid userId, out string subject, out string? body)
    {
        subject = string.Empty;
        body = null;
        if (!TryGuid(configuration, "userId", out userId)) return false;
        subject = Text(configuration, "subject") ?? string.Empty;
        body = Text(configuration, "body");
        return subject.Length > 0;
    }

    private static string? Text(string? configuration, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(configuration!);
            return document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGuid(string? configuration, string name, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(configuration)) return false;
        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out id)
                && id != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
