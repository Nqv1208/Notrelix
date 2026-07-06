namespace Notrelix.Domain.Common.Constants;

public static class AggregateTypes
{
    public const string User = "identity.user";
    public const string UserSession = "identity.user_session";
    public const string Workspace = "workspace.workspace";
    public const string WorkspaceMember = "workspace.workspace_member";
    public const string Board = "work.board";
    public const string BoardItem = "work.board_item";
    public const string BoardField = "work.board_field";
    public const string Page = "docs.page";
    public const string Block = "docs.block";
    public const string Comment = "collab.comment";
    public const string NotificationItem = "notifications.notification_item";
    public const string Plan = "billing.plan";
    public const string Subscription = "billing.subscription";
    public const string AutomationRule = "automation.automation_rule";
    public const string IntegrationConnection = "integration.integration_connection";
}
