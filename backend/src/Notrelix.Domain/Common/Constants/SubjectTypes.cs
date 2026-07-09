namespace Notrelix.Domain.Common.Constants;

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
