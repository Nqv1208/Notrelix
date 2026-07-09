using Notrelix.Domain.Accounts.Accounts.Events;
using Notrelix.Domain.Accounts.Invitations.Events;
using Notrelix.Domain.Accounts.Members.Events;
using Notrelix.Domain.Analytics.Dashboards.Events;
using Notrelix.Domain.Automation.Rules.Events;
using Notrelix.Domain.Automation.Executions.Events;
using Notrelix.Domain.Automation.Agents.Events;
using Notrelix.Domain.Automation.Scheduled.Events;
using Notrelix.Domain.Automation.Templates.Events;
using Notrelix.Domain.Billing.BillingEvents.Events;
using Notrelix.Domain.Billing.Entitlements.Events;
using Notrelix.Domain.Billing.Payments.Events;
using Notrelix.Domain.Billing.Plans.Events;
using Notrelix.Domain.Billing.Subscriptions.Events;
using Notrelix.Domain.Billing.Usage.Events;
using Notrelix.Domain.Collaboration.Attachments.Events;
using Notrelix.Domain.Collaboration.Comments.Events;
using Notrelix.Domain.Collaboration.Mentions.Events;
using Notrelix.Domain.Collaboration.Presence.Events;
using Notrelix.Domain.Collaboration.Reactions.Events;
using Notrelix.Domain.Collaboration.Watchers.Events;
using Notrelix.Domain.Documents.Blocks.Events;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Domain.Documents.ResourceLinks.Events;
using Notrelix.Domain.Documents.Templates.Events;
using Notrelix.Domain.Documents.Versions.Events;
using Notrelix.Domain.Governance.Policies.Events;
using Notrelix.Domain.Governance.Permissions.Events;
using Notrelix.Domain.Governance.Roles.Events;
using Notrelix.Domain.Governance.ShareLinks.Events;
using Notrelix.Domain.Governance.Templates.Events;
using Notrelix.Domain.Identity.Mfa.Events;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Identity.Profiles.Events;
using Notrelix.Domain.Identity.Security.Events;
using Notrelix.Domain.Identity.Sessions.Events;
using Notrelix.Domain.Identity.Tokens.Events;
using Notrelix.Domain.Identity.Users.Events;
using Notrelix.Domain.Integrations.Calendar.Events;
using Notrelix.Domain.Integrations.Connections.Events;
using Notrelix.Domain.Integrations.Sync.Events;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Notrelix.Domain.WorkManagement.Approvals.Events;
using Notrelix.Domain.WorkManagement.BoardGroups.Events;
using Notrelix.Domain.WorkManagement.Boards.Events;
using Notrelix.Domain.WorkManagement.Checklists.Events;
using Notrelix.Domain.WorkManagement.Fields.Events;
using Notrelix.Domain.WorkManagement.Forms.Events;
using Notrelix.Domain.WorkManagement.Formulas.Events;
using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.WorkManagement.Labels.Events;
using Notrelix.Domain.WorkManagement.Relations.Events;
using Notrelix.Domain.WorkManagement.Templates.Events;
using Notrelix.Domain.WorkManagement.Views.Events;
using Notrelix.Domain.Workspaces.Invitations.Events;
using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Spaces.Events;
using Notrelix.Domain.Workspaces.Teams.Events;
using Notrelix.Domain.Workspaces.Workspaces.Events;

namespace Notrelix.Infrastructure.Events;

public sealed class DomainEventDispatchPolicy : IDomainEventDispatchPolicy
{
    private static readonly Dictionary<Type, DomainEventDispatchMode> Policies = BuildPolicies();

    public DomainEventDispatchMode GetMode(Type eventType)
    {
        if (Policies.TryGetValue(eventType, out var mode))
            return mode;

        throw new InvalidOperationException(
            $"Domain event '{eventType.Name}' has no dispatch policy. " +
            "Every IDomainEvent must be explicitly registered in DomainEventDispatchPolicy.");
    }

    public IReadOnlyCollection<Type> GetInlineTypes()
    {
        return Policies
            .Where(kvp => kvp.Value == DomainEventDispatchMode.Inline)
            .Select(kvp => kvp.Key)
            .ToList()
            .AsReadOnly();
    }

    private static Dictionary<Type, DomainEventDispatchMode> BuildPolicies()
    {
        var d = new Dictionary<Type, DomainEventDispatchMode>();

        RegisterAccounts(d);
        RegisterAnalytics(d);
        RegisterAutomation(d);
        RegisterBilling(d);
        RegisterCollaboration(d);
        RegisterDocuments(d);
        RegisterGovernance(d);
        RegisterIdentity(d);
        RegisterIntegrations(d);
        RegisterWorkManagement(d);
        RegisterWorkspaces(d);

        return d;
    }

    private static void RegisterAccounts(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<AccountCreatedDomainEvent>(d);
        Add<AccountArchivedDomainEvent>(d);
        Add<AccountRenamedDomainEvent>(d);
        Add<AccountRestoredDomainEvent>(d);
        Add<AccountSoftDeletedDomainEvent>(d);
        Add<AccountSuspendedDomainEvent>(d);
        Add<AccountMemberActivatedDomainEvent>(d);
        Add<AccountMemberAddedDomainEvent>(d);
        Add<AccountMemberRemovedDomainEvent>(d);
        Add<AccountMemberRestoredDomainEvent>(d);
        Add<AccountMemberRoleChangedDomainEvent>(d);
        Add<AccountMemberSuspendedDomainEvent>(d);
        Add<AccountInvitationAcceptedDomainEvent>(d);
        Add<AccountInvitationCreatedDomainEvent>(d);
        Add<AccountInvitationExpiredDomainEvent>(d);
        Add<AccountInvitationRevokedDomainEvent>(d);
    }

    private static void RegisterAnalytics(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<DashboardCreatedDomainEvent>(d);
        Add<DashboardDeletedDomainEvent>(d);
        Add<DashboardRenamedDomainEvent>(d);
        Add<DashboardRestoredDomainEvent>(d);
        Add<DashboardSourceAddedDomainEvent>(d);
        Add<DashboardSourceUpdatedDomainEvent>(d);
        Add<DashboardVisibilityChangedDomainEvent>(d);
        Add<DashboardWidgetAddedDomainEvent>(d);
        Add<DashboardWidgetMovedDomainEvent>(d);
        Add<DashboardWidgetRemovedDomainEvent>(d);
    }

    private static void RegisterAutomation(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<AutomationConfigurationChangedDomainEvent>(d);
        Add<AutomationRuleCreatedDomainEvent>(d);
        Add<AutomationRuleDeletedDomainEvent>(d);
        Add<AutomationRuleDisabledDomainEvent>(d);
        Add<AutomationRuleEnabledDomainEvent>(d);
        Add<AutomationRuleRestoredDomainEvent>(d);
        Add<AutomationExecutionCancelledDomainEvent>(d);
        Add<AutomationExecutionFailedDomainEvent>(d);
        Add<AutomationExecutionQueuedDomainEvent>(d);
        Add<AutomationExecutionStartedDomainEvent>(d);
        Add<AutomationExecutionSucceededDomainEvent>(d);
        Add<AiAgentCreatedDomainEvent>(d);
        Add<AiAgentRunCancelledDomainEvent>(d);
        Add<AiAgentRunFailedDomainEvent>(d);
        Add<AiAgentRunQueuedDomainEvent>(d);
        Add<AiAgentRunStartedDomainEvent>(d);
        Add<AiAgentRunSucceededDomainEvent>(d);
        Add<AiAgentStatusChangedDomainEvent>(d);
        Add<AiAgentUpdatedDomainEvent>(d);
        Add<ScheduledJobCompletedDomainEvent>(d);
        Add<ScheduledJobCreatedDomainEvent>(d);
        Add<ScheduledJobFailedDomainEvent>(d);
        Add<ScheduledJobPausedDomainEvent>(d);
        Add<ScheduledJobRestoredDomainEvent>(d);
        Add<ScheduledJobRunCompletedDomainEvent>(d);
        Add<ScheduledJobSoftDeletedDomainEvent>(d);
        Add<ScheduledJobUpdatedDomainEvent>(d);
        Add<AutomationTemplateArchivedDomainEvent>(d);
        Add<AutomationTemplateCreatedDomainEvent>(d);
        Add<AutomationTemplatePublishedDomainEvent>(d);
        Add<AutomationTemplateRestoredDomainEvent>(d);
        Add<AutomationTemplateSoftDeletedDomainEvent>(d);
        Add<AutomationTemplateUpdatedDomainEvent>(d);
    }

    private static void RegisterBilling(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<BillingEventProcessedDomainEvent>(d);
        Add<BillingEventReceivedDomainEvent>(d);
        Add<EntitlementChangedDomainEvent>(d);
        Add<EntitlementDisabledDomainEvent>(d);
        Add<EntitlementExpiredDomainEvent>(d);
        Add<EntitlementGrantedDomainEvent>(d);
        Add<EntitlementLimitChangedDomainEvent>(d);
        Add<EntitlementRestoredDomainEvent>(d);
        Add<EntitlementRevokedDomainEvent>(d);
        Add<EntitlementSoftDeletedDomainEvent>(d);
        Add<InvoiceCreatedDomainEvent>(d);
        Add<InvoiceFailedDomainEvent>(d);
        Add<InvoiceIssuedDomainEvent>(d);
        Add<InvoicePaidDomainEvent>(d);
        Add<InvoiceRestoredDomainEvent>(d);
        Add<InvoiceSoftDeletedDomainEvent>(d);
        Add<InvoiceVoidedDomainEvent>(d);
        Add<PaymentMethodAddedDomainEvent>(d);
        Add<PlanArchivedDomainEvent>(d);
        Add<PlanCreatedDomainEvent>(d);
        Add<PlanDeprecatedDomainEvent>(d);
        Add<PlanDescriptionUpdatedDomainEvent>(d);
        Add<PlanLimitAddedDomainEvent>(d);
        Add<PlanRestoredDomainEvent>(d);
        Add<PlanSoftDeletedDomainEvent>(d);
        Add<SubscriptionActivatedDomainEvent>(d);
        Add<SubscriptionCanceledDomainEvent>(d);
        Add<SubscriptionCancellationScheduledDomainEvent>(d);
        Add<SubscriptionChangedDomainEvent>(d);
        Add<SubscriptionExpiredDomainEvent>(d);
        Add<SubscriptionPastDueDomainEvent>(d);
        Add<SubscriptionRenewedDomainEvent>(d);
        Add<SubscriptionRestoredDomainEvent>(d);
        Add<SubscriptionSoftDeletedDomainEvent>(d);
        Add<SubscriptionStartedDomainEvent>(d);
        Add<FeatureUsageConsumedDomainEvent>(d);
        Add<FeatureUsageReleasedDomainEvent>(d);
        Add<QuotaExceededDomainEvent>(d);
        Add<UsageLimitExceededDomainEvent>(d);
        Add<UsageMetricCreatedDomainEvent>(d);
        Add<UsageMetricDecreasedDomainEvent>(d);
        Add<UsageMetricIncreasedDomainEvent>(d);
        Add<UsageMetricResetDomainEvent>(d);
        Add<UsageMetricRestoredDomainEvent>(d);
        Add<UsageMetricSoftDeletedDomainEvent>(d);
        Add<WorkspaceFeatureUsageInitializedDomainEvent>(d);
        Add<WorkspaceFeatureUsageResetDomainEvent>(d);
        Add<WorkspaceFeatureUsageRestoredDomainEvent>(d);
        Add<WorkspaceFeatureUsageSoftDeletedDomainEvent>(d);
    }

    private static void RegisterCollaboration(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<AttachmentCreatedDomainEvent>(d);
        Add<AttachmentDeletedDomainEvent>(d);
        Add<AttachmentRestoredDomainEvent>(d);
        Add<CommentCreatedDomainEvent>(d);
        Add<CommentResolvedDomainEvent>(d);
        Add<CommentRestoredDomainEvent>(d);
        Add<CommentSoftDeletedDomainEvent>(d);
        Add<CommentUpdatedDomainEvent>(d);
        Add<MentionCreatedDomainEvent>(d);
        Add<PresenceUpdatedDomainEvent>(d);
        Add<ReactionCreatedDomainEvent>(d);
        Add<ReactionRemovedDomainEvent>(d);
        Add<ResourceWatchedDomainEvent>(d);
        Add<ResourceUnwatchedDomainEvent>(d);
    }

    private static void RegisterDocuments(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<BlockContentUpdatedDomainEvent>(d);
        Add<BlockCreatedDomainEvent>(d);
        Add<BlockMovedDomainEvent>(d);
        Add<BlockPropertiesUpdatedDomainEvent>(d);
        Add<BlockRestoredDomainEvent>(d);
        Add<BlockSoftDeletedDomainEvent>(d);
        Add<PageArchivedDomainEvent>(d);
        Add<PageCreatedDomainEvent>(d);
        Add<PageMovedDomainEvent>(d);
        Add<PageRenamedDomainEvent>(d);
        Add<PageRestoredDomainEvent>(d);
        Add<PageSoftDeletedDomainEvent>(d);
        Add<ResourceLinkCreatedDomainEvent>(d);
        Add<ResourceLinkDeletedDomainEvent>(d);
        Add<PageTemplateCreatedDomainEvent>(d);
        Add<PageTemplatePublishedDomainEvent>(d);
        Add<DocumentVersionCreatedDomainEvent>(d);
        Add<DocumentVersionRestoredDomainEvent>(d);
    }

    private static void RegisterGovernance(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<WorkspacePolicyUpdatedEvent>(d);
        Add<FieldPermissionGrantedDomainEvent>(d);
        Add<FieldPermissionRevokedDomainEvent>(d);
        Add<PermissionRuleCreatedDomainEvent>(d);
        Add<PermissionRuleDisabledDomainEvent>(d);
        Add<PermissionRuleRestoredDomainEvent>(d);
        Add<PermissionRuleSoftDeletedDomainEvent>(d);
        Add<ResourcePermissionGrantedDomainEvent>(d);
        Add<ResourcePermissionLevelChangedDomainEvent>(d);
        Add<ResourcePermissionRestoredDomainEvent>(d);
        Add<ResourcePermissionRevokedDomainEvent>(d);
        Add<ResourcePermissionSoftDeletedDomainEvent>(d);
        Add<CustomRoleActivatedDomainEvent>(d);
        Add<CustomRoleArchivedDomainEvent>(d);
        Add<CustomRoleAssignedDomainEvent>(d);
        Add<CustomRoleCreatedDomainEvent>(d);
        Add<CustomRoleRestoredDomainEvent>(d);
        Add<CustomRoleRevokedDomainEvent>(d);
        Add<CustomRoleSoftDeletedDomainEvent>(d);
        Add<CustomRoleUpdatedDomainEvent>(d);
        Add<ShareLinkCreatedEvent>(d);
        Add<ShareLinkDisabledEvent>(d);
        Add<ShareLinkExpiredEvent>(d);
        Add<ShareLinkRestoredEvent>(d);
        Add<ShareLinkRotatedEvent>(d);
        Add<ShareLinkSoftDeletedEvent>(d);
        Add<PermissionTemplateAppliedEvent>(d);
        Add<PermissionTemplateCreatedEvent>(d);
    }

    private static void RegisterIdentity(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<UserMfaMethodAddedDomainEvent>(d);
        Add<UserMfaMethodDisabledDomainEvent>(d);
        Add<UserMfaMethodRestoredDomainEvent>(d);
        Add<UserMfaMethodSetAsPrimaryDomainEvent>(d);
        Add<UserMfaMethodSoftDeletedDomainEvent>(d);
        Add<UserMfaMethodUnsetAsPrimaryDomainEvent>(d);
        Add<UserMfaMethodVerifiedDomainEvent>(d);
        Add<OAuthAccountLinkedDomainEvent>(d);
        Add<OAuthAccountUnlinkedDomainEvent>(d);
        Add<OAuthTokenReferenceRotatedDomainEvent>(d);
        Add<UserProfileCreatedDomainEvent>(d);
        Add<UserProfileUpdatedDomainEvent>(d);
        Add<LoginAttemptRecordedDomainEvent>(d);
        Add<PasswordChangeRequiredDomainEvent>(d);
        Add<UserMfaRequirementDisabledDomainEvent>(d);
        Add<UserMfaRequirementEnabledDomainEvent>(d);
        Add<UserSecurityPasswordChangedDomainEvent>(d);
        Add<UserSecuritySettingsCreatedDomainEvent>(d);
        Add<UserSecuritySettingsRestoredDomainEvent>(d);
        Add<UserSecuritySettingsSoftDeletedDomainEvent>(d);
        Add<UserSecuritySettingsUpdatedDomainEvent>(d);
        Add<UserSessionCreatedDomainEvent>(d);
        Add<UserSessionExpiredDomainEvent>(d);
        Add<UserSessionRefreshTokenRotatedDomainEvent>(d);
        Add<UserSessionRestoredDomainEvent>(d);
        Add<UserSessionRevokedDomainEvent>(d);
        Add<UserSessionSoftDeletedDomainEvent>(d);
        Add<ApiTokenCreatedDomainEvent>(d);
        Add<ApiTokenRestoredDomainEvent>(d);
        Add<ApiTokenRevokedDomainEvent>(d);
        Add<ApiTokenSoftDeletedDomainEvent>(d);
        Add<EmailVerificationTokenCreatedDomainEvent>(d);
        Add<EmailVerificationTokenExpiredDomainEvent>(d);
        Add<EmailVerificationTokenUsedDomainEvent>(d);
        Add<PasswordResetTokenCreatedDomainEvent>(d);
        Add<PasswordResetTokenExpiredDomainEvent>(d);
        Add<PasswordResetTokenUsedDomainEvent>(d);
        Add<UserActivatedDomainEvent>(d);
        Add<UserDeactivatedDomainEvent>(d);
        Add<UserEmailChangedDomainEvent>(d);
        Add<UserLoggedInDomainEvent>(d);
        Add<UserPasswordChangedDomainEvent>(d);
        Add<UserRegisteredDomainEvent>(d);
        Add<UserRestoredDomainEvent>(d);
        Add<UserSoftDeletedDomainEvent>(d);
        Add<UserSuspendedDomainEvent>(d);
    }

    private static void RegisterIntegrations(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<CalendarConflictDetectedDomainEvent>(d);
        Add<CalendarEventLinkedDomainEvent>(d);
        Add<CalendarIntegrationActivatedDomainEvent>(d);
        Add<CalendarIntegrationConnectedDomainEvent>(d);
        Add<CalendarIntegrationDeactivatedDomainEvent>(d);
        Add<CalendarIntegrationSyncDirectionChangedDomainEvent>(d);
        Add<CalendarSyncedDomainEvent>(d);
        Add<IntegrationConnectionCreatedDomainEvent>(d);
        Add<IntegrationConnectionExpiredDomainEvent>(d);
        Add<IntegrationConnectionReauthorizedDomainEvent>(d);
        Add<IntegrationConnectionRevokedDomainEvent>(d);
        Add<IntegrationScopeAddedDomainEvent>(d);
        Add<IntegrationScopeRemovedDomainEvent>(d);
        Add<IntegrationSecretRotatedDomainEvent>(d);
        Add<IntegrationSyncedDomainEvent>(d);
        Add<IntegrationSyncFailedDomainEvent>(d);
        Add<WebhookDeliveryRecordedDomainEvent>(d);
        Add<WebhookSubscriptionCreatedDomainEvent>(d);
        Add<WebhookSubscriptionEnabledDomainEvent>(d);
        Add<WebhookSubscriptionDisabledDomainEvent>(d);
        Add<WebhookSubscriptionSecretRotatedDomainEvent>(d);
    }

    private static void RegisterWorkManagement(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<ApprovalRequestApprovedDomainEvent>(d);
        Add<ApprovalRequestCancelledDomainEvent>(d);
        Add<ApprovalRequestCreatedDomainEvent>(d);
        Add<ApprovalRequestRejectedDomainEvent>(d);
        Add<ApprovalRequestRestoredDomainEvent>(d);
        Add<ApprovalRequestSoftDeletedDomainEvent>(d);
        Add<BoardGroupArchivedDomainEvent>(d);
        Add<BoardGroupColorChangedDomainEvent>(d);
        Add<BoardGroupCreatedDomainEvent>(d);
        Add<BoardGroupRenamedDomainEvent>(d);
        Add<BoardGroupReorderedDomainEvent>(d);
        Add<BoardGroupRestoredDomainEvent>(d);
        Add<BoardGroupSoftDeletedDomainEvent>(d);
        Add<BoardArchivedDomainEvent>(d);
        Add<BoardBackgroundUpdatedDomainEvent>(d);
        Add<BoardCreatedDomainEvent>(d);
        Add<BoardDefaultGroupSetDomainEvent>(d);
        Add<BoardDescriptionUpdatedDomainEvent>(d);
        Add<BoardItemIdentityGeneratedDomainEvent>(d);
        Add<BoardRenamedDomainEvent>(d);
        Add<BoardRestoredDomainEvent>(d);
        Add<BoardSoftDeletedDomainEvent>(d);
        Add<BoardUnarchivedDomainEvent>(d);
        Add<BoardVisibilityChangedDomainEvent>(d);
        Add<ChecklistCreatedDomainEvent>(d);
        Add<ChecklistItemAddedDomainEvent>(d);
        Add<ChecklistItemRemovedDomainEvent>(d);
        Add<ChecklistItemToggledDomainEvent>(d);
        Add<ChecklistRestoredDomainEvent>(d);
        Add<ChecklistSoftDeletedDomainEvent>(d);
        Add<BoardFieldClassificationUpdatedDomainEvent>(d);
        Add<BoardFieldCreatedDomainEvent>(d);
        Add<BoardFieldDeletedDomainEvent>(d);
        Add<BoardFieldFormulaUpdatedDomainEvent>(d);
        Add<BoardFieldRenamedDomainEvent>(d);
        Add<BoardFieldReorderedDomainEvent>(d);
        Add<BoardFieldRestoredDomainEvent>(d);
        Add<BoardFieldUpdatedDomainEvent>(d);
        Add<FieldOptionAddedDomainEvent>(d);
        Add<FieldOptionRemovedDomainEvent>(d);
        Add<FieldOptionUpdatedDomainEvent>(d);
        Add<FormClosedDomainEvent>(d);
        Add<FormCreatedDomainEvent>(d);
        Add<FormDetailsUpdatedDomainEvent>(d);
        Add<FormPublishedDomainEvent>(d);
        Add<FormQuestionAddedDomainEvent>(d);
        Add<FormRestoredDomainEvent>(d);
        Add<FormSoftDeletedDomainEvent>(d);
        Add<FormSubmissionCreatedDomainEvent>(d);
        Add<FormSubmissionMarkedAsSpamDomainEvent>(d);
        Add<FormSubmissionProcessedDomainEvent>(d);
        Add<FormSubmissionRejectedDomainEvent>(d);
        Add<FormulaDependencyChangedDomainEvent>(d);
        Add<BoardItemArchivedDomainEvent>(d);
        Add<BoardItemCompletedDomainEvent>(d);
        Add<BoardItemCreatedDomainEvent>(d);
        Add<BoardItemFieldValueChangedDomainEvent>(d);
        Add<BoardItemLabelAddedDomainEvent>(d);
        Add<BoardItemLabelRemovedDomainEvent>(d);
        Add<BoardItemLinkedDomainEvent>(d);
        Add<BoardItemMemberAssignedDomainEvent>(d);
        Add<BoardItemMemberUnassignedDomainEvent>(d);
        Add<BoardItemMovedDomainEvent>(d);
        Add<BoardItemParentAssignedDomainEvent>(d);
        Add<BoardItemRenamedDomainEvent>(d);
        Add<BoardItemRestoredDomainEvent>(d);
        Add<BoardItemSoftDeletedDomainEvent>(d);
        Add<BoardItemTimelineSetDomainEvent>(d);
        Add<LabelCreatedDomainEvent>(d);
        Add<LabelRestoredDomainEvent>(d);
        Add<LabelSoftDeletedDomainEvent>(d);
        Add<LabelUpdatedDomainEvent>(d);
        Add<BoardRelationCreatedDomainEvent>(d);
        Add<BoardRelationDeletedDomainEvent>(d);
        Add<BoardRelationMarkedBrokenDomainEvent>(d);
        Add<BoardRelationPausedDomainEvent>(d);
        Add<BoardRelationResumedDomainEvent>(d);
        Add<BoardRelationRestoredDomainEvent>(d);
        Add<RelationFieldConfiguredDomainEvent>(d);
        Add<BoardTemplateCreatedDomainEvent>(d);
        Add<ItemTemplateCreatedDomainEvent>(d);
        Add<BoardViewConfigUpdatedDomainEvent>(d);
        Add<BoardViewCreatedDomainEvent>(d);
        Add<BoardViewDeletedDomainEvent>(d);
        Add<BoardViewRenamedDomainEvent>(d);
        Add<BoardViewRestoredDomainEvent>(d);
        Add<BoardViewUserPreferenceCreatedDomainEvent>(d);
        Add<BoardViewUserPreferenceFilterChangedDomainEvent>(d);
        Add<BoardViewUserPreferenceGroupChangedDomainEvent>(d);
        Add<BoardViewUserPreferenceSortChangedDomainEvent>(d);
        Add<SavedFilterCreatedDomainEvent>(d);
        Add<SavedFilterFiltersUpdatedDomainEvent>(d);
        Add<SavedFilterGroupUpdatedDomainEvent>(d);
        Add<SavedFilterRenamedDomainEvent>(d);
        Add<SavedFilterRestoredDomainEvent>(d);
        Add<SavedFilterSoftDeletedDomainEvent>(d);
        Add<SavedFilterSortsUpdatedDomainEvent>(d);
        Add<SavedFilterVisibilityUpdatedDomainEvent>(d);
    }

    private static void RegisterWorkspaces(Dictionary<Type, DomainEventDispatchMode> d)
    {
        Add<WorkspaceInvitationAcceptedDomainEvent>(d);
        Add<WorkspaceInvitationCreatedDomainEvent>(d);
        Add<WorkspaceInvitationExpiredDomainEvent>(d);
        Add<WorkspaceInvitationRevokedDomainEvent>(d);
        Add<WorkspaceMemberActivatedDomainEvent>(d);
        Add<WorkspaceMemberAddedDomainEvent>(d);
        Add<WorkspaceMemberRemovedDomainEvent>(d);
        Add<WorkspaceMemberRestoredDomainEvent>(d);
        Add<WorkspaceMemberRoleChangedDomainEvent>(d);
        Add<WorkspaceMemberSuspendedDomainEvent>(d);
        Add<SpaceArchivedDomainEvent>(d);
        Add<SpaceCreatedDomainEvent>(d);
        Add<SpaceMovedDomainEvent>(d);
        Add<SpaceRenamedDomainEvent>(d);
        Add<SpaceRestoredDomainEvent>(d);
        Add<SpaceSoftDeletedDomainEvent>(d);
        Add<TeamArchivedDomainEvent>(d);
        Add<TeamCreatedDomainEvent>(d);
        Add<TeamMemberAddedDomainEvent>(d);
        Add<TeamMemberRemovedDomainEvent>(d);
        Add<TeamRenamedDomainEvent>(d);
        Add<TeamRestoredDomainEvent>(d);
        Add<TeamSoftDeletedDomainEvent>(d);
        Add<WorkspaceArchivedDomainEvent>(d);
        Add<WorkspaceCreatedDomainEvent>(d);
        Add<WorkspaceRenamedDomainEvent>(d);
        Add<WorkspaceDescriptionUpdatedDomainEvent>(d);
        Add<WorkspaceRestoredDomainEvent>(d);
        Add<WorkspaceSoftDeletedDomainEvent>(d);
    }

    private static void Add<T>(Dictionary<Type, DomainEventDispatchMode> d, DomainEventDispatchMode mode = DomainEventDispatchMode.Outbox)
        where T : Domain.Common.IDomainEvent
    {
        d[typeof(T)] = mode;
    }
}
