namespace Notrelix.Domain.Common.Constants;

public static class SourceContexts
{
    public const string Identity = "identity";
    public const string Accounts = "accounts";
    public const string Workspaces = "workspaces";
    public const string Governance = "governance";
    public const string Work = "work";
    public const string Docs = "docs";
    public const string Collaboration = "collaboration";
    public const string Notifications = "notifications";
    public const string Automation = "automation";
    public const string Integrations = "integrations";
    public const string Billing = "billing";
    public const string Analytics = "analytics";

    // Legacy alias used by Infrastructure outbox. Prefer Integrations.
    public const string Integration = "integration";
}
