namespace Notrelix.Infrastructure.Data.Messaging;

/// <summary>
/// Infrastructure-owned transport constants for outbox messaging.
/// These are NOT domain concepts — they are serialization/mapping strings for the outbox.
/// Domain does not reference these constants.
/// </summary>
public static class OutboxConstants
{
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
        public const string Integration = "integration";
    }

    public static class AggregateTypes
    {
        public const string User = "user";
        public const string Account = "account";
        public const string UserSession = "user_session";
        public const string Workspace = "workspace";
        public const string WorkspaceMember = "workspace_member";
        public const string Board = "board";
        public const string BoardItem = "board_item";
        public const string BoardField = "board_field";
        public const string Page = "page";
        public const string Block = "block";
        public const string Comment = "comment";
        public const string NotificationItem = "notification_item";
        public const string Plan = "plan";
        public const string Subscription = "subscription";
        public const string AutomationRule = "automation_rule";
        public const string IntegrationConnection = "integration_connection";
    }

    public static class SubjectTypes
    {
        public const string User = AggregateTypes.User;
        public const string Account = AggregateTypes.Account;
        public const string UserSession = AggregateTypes.UserSession;
        public const string Workspace = AggregateTypes.Workspace;
        public const string WorkspaceMember = AggregateTypes.WorkspaceMember;
        public const string Board = AggregateTypes.Board;
        public const string BoardItem = AggregateTypes.BoardItem;
        public const string Page = AggregateTypes.Page;
        public const string Block = AggregateTypes.Block;
        public const string Comment = AggregateTypes.Comment;
        public const string Mention = "mention";
        public const string Reaction = "reaction";
        public const string NotificationItem = AggregateTypes.NotificationItem;
        public const string EmailOutbox = "email_outbox";
        public const string Subscription = AggregateTypes.Subscription;
        public const string AutomationRule = AggregateTypes.AutomationRule;
        public const string IntegrationConnection = AggregateTypes.IntegrationConnection;
    }
}
