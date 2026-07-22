namespace Notrelix.Application.Common.Messaging;

public static class ConsumerNames
{
    // Identity
    public const string UserRegistered = "identity.user-registered.v1";
    public const string EmailVerificationDelivery = "identity.email-verification-delivery.v1";
    public const string UserDeactivated = "identity.user-deactivated.v1";
    public const string PersonalWorkspaceProvisioning = "workspaces.personal-workspace-provisioning.v1";
    public const string WelcomeEmailSending = "identity.welcome-email-sending.v1";

    // Accounts
    public const string AccountCreated = "account.created.v1";

    // Workspaces
    public const string WorkspaceCreated = "workspaces.workspace-created.v1";
    public const string WorkspaceArchived = "workspaces.workspace-archived.v1";
    public const string WorkspaceUnarchived = "workspaces.workspace-unarchived.v1";
    public const string WorkspaceMemberAdded = "workspaces.workspace-member-added.v1";
    public const string WorkspaceMemberRemoved = "workspaces.workspace-member-removed.v1";
    public const string InvitationDelivery = "workspaces.invitation-delivery.v1";
    public const string TeamCreated = "workspaces.team-created.v1";
    public const string SpaceCreated = "workspaces.space-created.v1";

    // Work Management
    public const string BoardCreated = "work.board-created.v1";
    public const string BoardRenamed = "work.board-renamed.v1";
    public const string BoardArchived = "work.board-archived.v1";
    public const string BoardUnarchived = "work.board-unarchived.v1";
    public const string BoardItemCreated = "work.board-item-created.v1";
    public const string BoardItemRenamed = "work.board-item-renamed.v1";
    public const string BoardItemMoved = "work.board-item-moved.v1";
    public const string BoardItemArchived = "work.board-item-archived.v1";
    public const string BoardItemFieldValueChanged = "work.board-item-field-value-changed.v1";
    public const string BoardFieldCreated = "work.board-field-created.v1";
    public const string BoardFieldUpdated = "work.board-field-updated.v1";
    public const string BoardFieldDeleted = "work.board-field-deleted.v1";
    public const string BoardViewCreated = "work.board-view-created.v1";
    public const string BoardViewDeleted = "work.board-view-deleted.v1";
    public const string LabelCreated = "work.label-created.v1";
    public const string LabelUpdated = "work.label-updated.v1";
    public const string ChecklistCreated = "work.checklist-created.v1";
    public const string ChecklistItemToggled = "work.checklist-item-toggled.v1";

    // Documents
    public const string PageCreated = "document.page-created.v1";
    public const string PageArchived = "document.page-archived.v1";

    // Governance
    public const string CustomRoleAssigned = "governance.role-assigned.v1";
    public const string ResourcePermissionGranted = "governance.permission-granted.v1";
    public const string ResourcePermissionRevoked = "governance.permission-revoked.v1";

    // Collaboration
    public const string BoardCreatedActivity = "collaboration.board-created-activity.v1";
    public const string CommentCreatedActivity = "collaboration.comment-created-activity.v1";
    public const string MentionCreatedActivity = "collaboration.mention-created-activity.v1";
    public const string WorkspaceMemberAddedActivity = "collaboration.workspace-member-added-activity.v1";
    public const string MentionCreatedNotification = "collaboration.mention-created-notification.v1";

    // Billing
    public const string SubscriptionChanged = "billing.subscription-changed.v1";
    public const string SubscriptionCanceled = "billing.subscription-canceled.v1";
}
