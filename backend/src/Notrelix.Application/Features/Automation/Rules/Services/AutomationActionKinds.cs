namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>Which action types reach an external integration (and so need ManageIntegrations).</summary>
public static class AutomationActionKinds
{
    public static bool IsIntegration(string? type) =>
        string.Equals(type, "Webhook", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "SlackMessage", StringComparison.OrdinalIgnoreCase);
}
