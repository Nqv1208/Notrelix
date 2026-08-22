namespace Notrelix.Infrastructure.Messaging;

public static class ConsumerRegistrySetup
{
    public static IReadOnlyList<ConsumerDefinition> GetConsumerDefinitions()
    {
        return
        [
            // ── Identity (5) ─────────────────────────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "UserRegisteredConsumer",
                EventName = "identity.user-registered",
                EventVersion = 2,
                EndpointName = "notrelix-identity-user-registered-v1",
                BoundedContext = "Identity",
                Description = "Handles user registration",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceProvisioningConsumer",
                EventName = "identity.registration-completed",
                EventVersion = 1,
                EndpointName = "notrelix-identity-registration-completed-v1",
                BoundedContext = "Identity",
                Description = "Provisions workspace after registration",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "SendWelcomeEmailConsumer",
                EventName = "identity.registration-completed",
                EventVersion = 1,
                EndpointName = "notrelix-identity-send-welcome-email-v1",
                BoundedContext = "Identity",
                Description = "Sends welcome email after registration",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "SendEmailVerificationEmailConsumer",
                EventName = "identity.email-verification-delivery-requested",
                EventVersion = 1,
                EndpointName = "notrelix-identity-email-verification-v1",
                BoundedContext = "Identity",
                Description = "Sends email verification link",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "UserDeactivatedConsumer",
                EventName = "user.deactivated",
                EventVersion = 1,
                EndpointName = "notrelix-identity-user-deactivated-v1",
                BoundedContext = "Identity",
                Description = "Handles user deactivation",
                Maturity = ConsumerMaturity.Implemented
            },

            // ── Workspaces (4) ───────────────────────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceCreatedConsumer",
                EventName = "workspace.created",
                EventVersion = 1,
                EndpointName = "notrelix-workspace-created-v1",
                BoundedContext = "Workspaces",
                Description = "Handles workspace creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceMemberAddedConsumer",
                EventName = "workspace.member.added",
                EventVersion = 1,
                EndpointName = "notrelix-workspace-member-added-v1",
                BoundedContext = "Workspaces",
                Description = "Handles workspace member addition",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceMemberRemovedConsumer",
                EventName = "workspace.member.removed",
                EventVersion = 1,
                EndpointName = "notrelix-workspace-member-removed-v1",
                BoundedContext = "Workspaces",
                Description = "Handles workspace member removal",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "SendInvitationEmailConsumer",
                EventName = "workspaces.invitation-delivery-requested",
                EventVersion = 1,
                EndpointName = "notrelix-workspaces-invitation-delivery-v1",
                BoundedContext = "Workspaces",
                Description = "Sends workspace invitation email",
                Maturity = ConsumerMaturity.Implemented
            },

            // ── WorkManagement (3) ───────────────────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "BoardCreatedConsumer",
                EventName = "board.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Handles board creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardItemCreatedConsumer",
                EventName = "board.item.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-item-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Handles board item creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardItemFieldValueChangedConsumer",
                EventName = "board.item.field_value.changed",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-item-field-value-changed-v1",
                BoundedContext = "WorkManagement",
                Description = "Handles board item field value changes",
                Maturity = ConsumerMaturity.Implemented
            },

            // ── Collaboration / Activity (4) ─────────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "BoardCreatedActivityConsumer",
                EventName = "board.created",
                EventVersion = 1,
                EndpointName = "notrelix-activity-board-created-v1",
                BoundedContext = "Collaboration",
                Description = "Records activity for board creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "CommentCreatedActivityConsumer",
                EventName = "comment.created",
                EventVersion = 1,
                EndpointName = "notrelix-activity-comment-created-v1",
                BoundedContext = "Collaboration",
                Description = "Records activity for comment creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "MentionCreatedActivityConsumer",
                EventName = "mention.created",
                EventVersion = 1,
                EndpointName = "notrelix-activity-mention-created-v1",
                BoundedContext = "Collaboration",
                Description = "Records activity for mention creation",
                Maturity = ConsumerMaturity.Implemented
            },
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceMemberAddedActivityConsumer",
                EventName = "workspace.member.added",
                EventVersion = 1,
                EndpointName = "notrelix-activity-member-added-v1",
                BoundedContext = "Collaboration",
                Description = "Records activity for member addition",
                Maturity = ConsumerMaturity.Implemented
            },

            // ── Collaboration / Notifications (1) ────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "MentionCreatedNotificationConsumer",
                EventName = "mention.created",
                EventVersion = 1,
                EndpointName = "notrelix-notification-mention-created-v1",
                BoundedContext = "Collaboration",
                Description = "Sends notification for mention creation",
                Maturity = ConsumerMaturity.Implemented
            },

            // ── Billing (1) ──────────────────────────────────────────────
            new ConsumerDefinition
            {
                ConsumerName = "SubscriptionChangedConsumer",
                EventName = "subscription.changed",
                EventVersion = 1,
                EndpointName = "notrelix-billing-subscription-changed-v1",
                BoundedContext = "Billing",
                Description = "Handles subscription change",
                Maturity = ConsumerMaturity.Implemented
            },

            // ══════════════════════════════════════════════════════════════
            // ── Stub consumers (26) ──────────────────────────────────────
            // ══════════════════════════════════════════════════════════════

            // Accounts
            new ConsumerDefinition
            {
                ConsumerName = "AccountCreated",
                EventName = "account.created",
                EventVersion = 1,
                EndpointName = "notrelix-account-created-v1",
                BoundedContext = "Accounts",
                Description = "Account created",
                Maturity = ConsumerMaturity.Stub
            },

            // Workspaces (stubs)
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceArchived",
                EventName = "workspace.archived",
                EventVersion = 1,
                EndpointName = "notrelix-workspace-archived-v1",
                BoundedContext = "Workspaces",
                Description = "Workspace archived",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "WorkspaceUnarchived",
                EventName = "workspace.unarchived",
                EventVersion = 1,
                EndpointName = "notrelix-workspace-unarchived-v1",
                BoundedContext = "Workspaces",
                Description = "Workspace restored",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "TeamCreated",
                EventName = "team.created",
                EventVersion = 1,
                EndpointName = "notrelix-team-created-v1",
                BoundedContext = "Workspaces",
                Description = "Team created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "SpaceCreated",
                EventName = "space.created",
                EventVersion = 1,
                EndpointName = "notrelix-space-created-v1",
                BoundedContext = "Workspaces",
                Description = "Space created",
                Maturity = ConsumerMaturity.Stub
            },

            // WorkManagement (stubs)
            new ConsumerDefinition
            {
                ConsumerName = "BoardRenamed",
                EventName = "board.renamed",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-renamed-v1",
                BoundedContext = "WorkManagement",
                Description = "Board renamed",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardArchived",
                EventName = "board.archived",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-archived-v1",
                BoundedContext = "WorkManagement",
                Description = "Board archived",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardUnarchived",
                EventName = "board.unarchived",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-unarchived-v1",
                BoundedContext = "WorkManagement",
                Description = "Board restored",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardItemRenamed",
                EventName = "board_item.renamed",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-item-renamed-v1",
                BoundedContext = "WorkManagement",
                Description = "Board item renamed",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardItemMoved",
                EventName = "board_item.moved",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-item-moved-v1",
                BoundedContext = "WorkManagement",
                Description = "Board item moved",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardItemArchived",
                EventName = "board_item.archived",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-item-archived-v1",
                BoundedContext = "WorkManagement",
                Description = "Board item archived",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardFieldCreated",
                EventName = "board_field.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-field-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Board field created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardFieldUpdated",
                EventName = "board_field.updated",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-field-updated-v1",
                BoundedContext = "WorkManagement",
                Description = "Board field updated",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardFieldDeleted",
                EventName = "board_field.deleted",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-field-deleted-v1",
                BoundedContext = "WorkManagement",
                Description = "Board field deleted",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardViewCreated",
                EventName = "board_view.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-view-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Board view created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "BoardViewDeleted",
                EventName = "board_view.deleted",
                EventVersion = 1,
                EndpointName = "notrelix-work-board-view-deleted-v1",
                BoundedContext = "WorkManagement",
                Description = "Board view deleted",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "LabelCreated",
                EventName = "label.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-label-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Label created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "LabelUpdated",
                EventName = "label.updated",
                EventVersion = 1,
                EndpointName = "notrelix-work-label-updated-v1",
                BoundedContext = "WorkManagement",
                Description = "Label updated",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "ChecklistCreated",
                EventName = "checklist.created",
                EventVersion = 1,
                EndpointName = "notrelix-work-checklist-created-v1",
                BoundedContext = "WorkManagement",
                Description = "Checklist created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "ChecklistItemToggled",
                EventName = "checklist_item.toggled",
                EventVersion = 1,
                EndpointName = "notrelix-work-checklist-item-toggled-v1",
                BoundedContext = "WorkManagement",
                Description = "Checklist item toggled",
                Maturity = ConsumerMaturity.Stub
            },

            // Documents
            new ConsumerDefinition
            {
                ConsumerName = "PageCreated",
                EventName = "page.created",
                EventVersion = 1,
                EndpointName = "notrelix-doc-page-created-v1",
                BoundedContext = "Documents",
                Description = "Page created",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "PageArchived",
                EventName = "page.archived",
                EventVersion = 1,
                EndpointName = "notrelix-doc-page-archived-v1",
                BoundedContext = "Documents",
                Description = "Page archived",
                Maturity = ConsumerMaturity.Stub
            },

            // Governance
            new ConsumerDefinition
            {
                ConsumerName = "CustomRoleAssigned",
                EventName = "governance.role.assigned",
                EventVersion = 1,
                EndpointName = "notrelix-governance-role-assigned-v1",
                BoundedContext = "Governance",
                Description = "Custom role assigned",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "ResourcePermissionGranted",
                EventName = "governance.permission.granted",
                EventVersion = 1,
                EndpointName = "notrelix-governance-permission-granted-v1",
                BoundedContext = "Governance",
                Description = "Resource permission granted",
                Maturity = ConsumerMaturity.Stub
            },
            new ConsumerDefinition
            {
                ConsumerName = "ResourcePermissionRevoked",
                EventName = "governance.permission.revoked",
                EventVersion = 1,
                EndpointName = "notrelix-governance-permission-revoked-v1",
                BoundedContext = "Governance",
                Description = "Resource permission revoked",
                Maturity = ConsumerMaturity.Stub
            },

            // Billing (stub)
            new ConsumerDefinition
            {
                ConsumerName = "SubscriptionCanceled",
                EventName = "subscription.canceled",
                EventVersion = 1,
                EndpointName = "notrelix-billing-subscription-canceled-v1",
                BoundedContext = "Billing",
                Description = "Subscription canceled",
                Maturity = ConsumerMaturity.Stub
            }
        ];
    }
}
