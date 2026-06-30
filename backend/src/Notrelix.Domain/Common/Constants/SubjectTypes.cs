namespace Notrelix.Domain.Common.Constants;

public static class SubjectTypes
{
    public const string User = "identity.user";
    public const string UserSession = "identity.user_session";
    public const string Workspace = "workspace.workspace";
    public const string WorkspaceMember = "workspace.workspace_member";
    public const string Board = "work.board";
    public const string BoardItem = "work.board_item";
    public const string Page = "docs.page";
    public const string Block = "docs.block";
    public const string Comment = "collab.comment";
    public const string Mention = "collab.mention";
    public const string Reaction = "collab.reaction";
    public const string NotificationItem = "notifications.notification_item";
    public const string EmailOutbox = "notifications.email_outbox";
    public const string Subscription = "billing.subscription";
    public const string AutomationRule = "automation.automation_rule";
    public const string IntegrationConnection = "integration.integration_connection";
}
