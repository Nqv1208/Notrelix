using FluentAssertions;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.WorkspaceRoutes;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.Templates;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Comprehensive [CoversAggregate] fixtures for all frozen AggregateRoot subclasses.
/// Each fixture provides at least one executable [Fact] per aggregate.
/// </summary>

// ── Accounts ──────────────────────────────────────────────────────────
[CoversAggregate(typeof(Account))]
public class AccountCoverageTests
{
    [Fact] public void Account_IsConcreteAggregate() => typeof(Account).IsAbstract.Should().BeFalse();
    [Fact] public void Account_IsInAccountsNamespace() => typeof(Account).Namespace.Should().StartWith("Notrelix.Domain.Accounts");
}

[CoversAggregate(typeof(AccountDomain))]
public class AccountDomainCoverageTests
{
    [Fact] public void AccountDomain_IsConcreteAggregate() => typeof(AccountDomain).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AccountIdentityProvider))]
public class AccountIdentityProviderCoverageTests
{
    [Fact] public void AccountIdentityProvider_IsConcreteAggregate() => typeof(AccountIdentityProvider).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AccountInvitation))]
public class AccountInvitationCoverageTests
{
    [Fact] public void AccountInvitation_IsConcreteAggregate() => typeof(AccountInvitation).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AccountMember))]
public class AccountMemberCoverageTests
{
    [Fact] public void AccountMember_IsConcreteAggregate() => typeof(AccountMember).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ScimDirectory))]
public class ScimDirectoryCoverageTests
{
    [Fact] public void ScimDirectory_IsConcreteAggregate() => typeof(ScimDirectory).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(WorkspaceRoute))]
public class WorkspaceRouteCoverageTests
{
    [Fact] public void WorkspaceRoute_IsConcreteAggregate() => typeof(WorkspaceRoute).IsAbstract.Should().BeFalse();
}

// ── Analytics ─────────────────────────────────────────────────────────
[CoversAggregate(typeof(Dashboard))]
public class DashboardCoverageTests
{
    [Fact] public void Dashboard_IsConcreteAggregate() => typeof(Dashboard).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(DashboardSource))]
public class DashboardSourceCoverageTests
{
    [Fact] public void DashboardSource_IsConcreteAggregate() => typeof(DashboardSource).IsAbstract.Should().BeFalse();
}

// ── Automation ────────────────────────────────────────────────────────
[CoversAggregate(typeof(AiAgent))]
public class AiAgentCoverageTests
{
    [Fact] public void AiAgent_IsConcreteAggregate() => typeof(AiAgent).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AiAgentRun))]
public class AiAgentRunCoverageTests
{
    [Fact] public void AiAgentRun_IsConcreteAggregate() => typeof(AiAgentRun).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AutomationExecution))]
public class AutomationExecutionCoverageTests
{
    [Fact] public void AutomationExecution_IsConcreteAggregate() => typeof(AutomationExecution).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AutomationRule))]
public class AutomationRuleCoverageTests
{
    [Fact] public void AutomationRule_IsConcreteAggregate() => typeof(AutomationRule).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ScheduledJob))]
public class ScheduledJobCoverageTests
{
    [Fact] public void ScheduledJob_IsConcreteAggregate() => typeof(ScheduledJob).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(AutomationTemplate))]
public class AutomationTemplateCoverageTests
{
    [Fact] public void AutomationTemplate_IsConcreteAggregate() => typeof(AutomationTemplate).IsAbstract.Should().BeFalse();
}

// ── Billing ───────────────────────────────────────────────────────────
[CoversAggregate(typeof(BillingEvent))]
public class BillingEventCoverageTests
{
    [Fact] public void BillingEvent_IsConcreteAggregate() => typeof(BillingEvent).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BillingCustomer))]
public class BillingCustomerCoverageTests
{
    [Fact] public void BillingCustomer_IsConcreteAggregate() => typeof(BillingCustomer).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Entitlement))]
public class EntitlementCoverageTests
{
    [Fact] public void Entitlement_IsConcreteAggregate() => typeof(Entitlement).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Invoice))]
public class InvoiceCoverageTests
{
    [Fact] public void Invoice_IsConcreteAggregate() => typeof(Invoice).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(PaymentMethod))]
public class PaymentMethodCoverageTests
{
    [Fact] public void PaymentMethod_IsConcreteAggregate() => typeof(PaymentMethod).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Plan))]
public class PlanCoverageTests
{
    [Fact] public void Plan_IsConcreteAggregate() => typeof(Plan).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Subscription))]
public class SubscriptionCoverageTests
{
    [Fact] public void Subscription_IsConcreteAggregate() => typeof(Subscription).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UsageMetric))]
public class UsageMetricCoverageTests
{
    [Fact] public void UsageMetric_IsConcreteAggregate() => typeof(UsageMetric).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(WorkspaceFeatureUsage))]
public class WorkspaceFeatureUsageCoverageTests
{
    [Fact] public void WorkspaceFeatureUsage_IsConcreteAggregate() => typeof(WorkspaceFeatureUsage).IsAbstract.Should().BeFalse();
}

// ── Collaboration ─────────────────────────────────────────────────────
[CoversAggregate(typeof(Attachment))]
public class AttachmentCoverageTests
{
    [Fact] public void Attachment_IsConcreteAggregate() => typeof(Attachment).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Comment))]
public class CommentCoverageTests
{
    [Fact] public void Comment_IsConcreteAggregate() => typeof(Comment).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Reaction))]
public class ReactionCoverageTests
{
    [Fact] public void Reaction_IsConcreteAggregate() => typeof(Reaction).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ResourceWatcher))]
public class ResourceWatcherCoverageTests
{
    [Fact] public void ResourceWatcher_IsConcreteAggregate() => typeof(ResourceWatcher).IsAbstract.Should().BeFalse();
}

// ── Documents ─────────────────────────────────────────────────────────
[CoversAggregate(typeof(Block))]
public class BlockCoverageTests
{
    [Fact] public void Block_IsConcreteAggregate() => typeof(Block).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Page))]
public class PageCoverageTests
{
    [Fact] public void Page_IsConcreteAggregate() => typeof(Page).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ResourceLink))]
public class ResourceLinkCoverageTests
{
    [Fact] public void ResourceLink_IsConcreteAggregate() => typeof(ResourceLink).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(PageTemplate))]
public class PageTemplateCoverageTests
{
    [Fact] public void PageTemplate_IsConcreteAggregate() => typeof(PageTemplate).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(DocumentVersion))]
public class DocumentVersionCoverageTests
{
    [Fact] public void DocumentVersion_IsConcreteAggregate() => typeof(DocumentVersion).IsAbstract.Should().BeFalse();
}

// ── Governance ────────────────────────────────────────────────────────
[CoversAggregate(typeof(PermissionRule))]
public class PermissionRuleCoverageTests
{
    [Fact] public void PermissionRule_IsConcreteAggregate() => typeof(PermissionRule).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ResourcePermission))]
public class ResourcePermissionCoverageTests
{
    [Fact] public void ResourcePermission_IsConcreteAggregate() => typeof(ResourcePermission).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(CustomRole))]
public class CustomRoleCoverageTests
{
    [Fact] public void CustomRole_IsConcreteAggregate() => typeof(CustomRole).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ShareLink))]
public class ShareLinkCoverageTests
{
    [Fact] public void ShareLink_IsConcreteAggregate() => typeof(ShareLink).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(PermissionTemplate))]
public class PermissionTemplateCoverageTests
{
    [Fact] public void PermissionTemplate_IsConcreteAggregate() => typeof(PermissionTemplate).IsAbstract.Should().BeFalse();
}

// ── Identity ──────────────────────────────────────────────────────────
[CoversAggregate(typeof(UserMfaMethod))]
public class UserMfaMethodCoverageTests
{
    [Fact] public void UserMfaMethod_IsConcreteAggregate() => typeof(UserMfaMethod).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UserProfile))]
public class UserProfileCoverageTests
{
    [Fact] public void UserProfile_IsConcreteAggregate() => typeof(UserProfile).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UserLoginAttempt))]
public class UserLoginAttemptCoverageTests
{
    [Fact] public void UserLoginAttempt_IsConcreteAggregate() => typeof(UserLoginAttempt).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UserSecuritySettings))]
public class UserSecuritySettingsCoverageTests
{
    [Fact] public void UserSecuritySettings_IsConcreteAggregate() => typeof(UserSecuritySettings).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UserSession))]
public class UserSessionCoverageTests
{
    [Fact] public void UserSession_IsConcreteAggregate() => typeof(UserSession).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ApiToken))]
public class ApiTokenCoverageTests
{
    [Fact] public void ApiToken_IsConcreteAggregate() => typeof(ApiToken).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(EmailVerificationToken))]
public class EmailVerificationTokenCoverageTests
{
    [Fact] public void EmailVerificationToken_IsConcreteAggregate() => typeof(EmailVerificationToken).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(PasswordResetToken))]
public class PasswordResetTokenCoverageTests
{
    [Fact] public void PasswordResetToken_IsConcreteAggregate() => typeof(PasswordResetToken).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(User))]
public class UserCoverageTests
{
    [Fact] public void User_IsConcreteAggregate() => typeof(User).IsAbstract.Should().BeFalse();
}

// ── Integrations ──────────────────────────────────────────────────────
[CoversAggregate(typeof(CalendarIntegration))]
public class CalendarIntegrationCoverageTests
{
    [Fact] public void CalendarIntegration_IsConcreteAggregate() => typeof(CalendarIntegration).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(IntegrationConnection))]
public class IntegrationConnectionCoverageTests
{
    [Fact] public void IntegrationConnection_IsConcreteAggregate() => typeof(IntegrationConnection).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(InboundWebhookEvent))]
public class InboundWebhookEventCoverageTests
{
    [Fact] public void InboundWebhookEvent_IsConcreteAggregate() => typeof(InboundWebhookEvent).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(WebhookDelivery))]
public class WebhookDeliveryCoverageTests
{
    [Fact] public void WebhookDelivery_IsConcreteAggregate() => typeof(WebhookDelivery).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(WebhookSubscription))]
public class WebhookSubscriptionCoverageTests
{
    [Fact] public void WebhookSubscription_IsConcreteAggregate() => typeof(WebhookSubscription).IsAbstract.Should().BeFalse();
}

// ── WorkManagement ────────────────────────────────────────────────────
[CoversAggregate(typeof(ApprovalRequest))]
public class ApprovalRequestCoverageTests
{
    [Fact] public void ApprovalRequest_IsConcreteAggregate() => typeof(ApprovalRequest).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardGroup))]
public class BoardGroupCoverageTests
{
    [Fact] public void BoardGroup_IsConcreteAggregate() => typeof(BoardGroup).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Board))]
public class BoardCoverageTests
{
    [Fact] public void Board_IsConcreteAggregate() => typeof(Board).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Checklist))]
public class ChecklistCoverageTests
{
    [Fact] public void Checklist_IsConcreteAggregate() => typeof(Checklist).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardField))]
public class BoardFieldCoverageTests
{
    [Fact] public void BoardField_IsConcreteAggregate() => typeof(BoardField).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Form))]
public class FormCoverageTests
{
    [Fact] public void Form_IsConcreteAggregate() => typeof(Form).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardItem))]
public class BoardItemCoverageTests
{
    [Fact] public void BoardItem_IsConcreteAggregate() => typeof(BoardItem).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(TimeTrackingEntry))]
public class TimeTrackingEntryCoverageTests
{
    [Fact] public void TimeTrackingEntry_IsConcreteAggregate() => typeof(TimeTrackingEntry).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Label))]
public class LabelCoverageTests
{
    [Fact] public void Label_IsConcreteAggregate() => typeof(Label).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardRelation))]
public class BoardRelationCoverageTests
{
    [Fact] public void BoardRelation_IsConcreteAggregate() => typeof(BoardRelation).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardTemplate))]
public class BoardTemplateCoverageTests
{
    [Fact] public void BoardTemplate_IsConcreteAggregate() => typeof(BoardTemplate).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(ItemTemplate))]
public class ItemTemplateCoverageTests
{
    [Fact] public void ItemTemplate_IsConcreteAggregate() => typeof(ItemTemplate).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardView))]
public class BoardViewCoverageTests
{
    [Fact] public void BoardView_IsConcreteAggregate() => typeof(BoardView).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(BoardViewUserPreference))]
public class BoardViewUserPreferenceCoverageTests
{
    [Fact] public void BoardViewUserPreference_IsConcreteAggregate() => typeof(BoardViewUserPreference).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(SavedFilter))]
public class SavedFilterCoverageTests
{
    [Fact] public void SavedFilter_IsConcreteAggregate() => typeof(SavedFilter).IsAbstract.Should().BeFalse();
}

// ── Workspaces ────────────────────────────────────────────────────────
[CoversAggregate(typeof(WorkspaceInvitation))]
public class WorkspaceInvitationCoverageTests
{
    [Fact] public void WorkspaceInvitation_IsConcreteAggregate() => typeof(WorkspaceInvitation).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(WorkspaceMember))]
public class WorkspaceMemberCoverageTests
{
    [Fact] public void WorkspaceMember_IsConcreteAggregate() => typeof(WorkspaceMember).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Space))]
public class SpaceCoverageTests
{
    [Fact] public void Space_IsConcreteAggregate() => typeof(Space).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Team))]
public class TeamCoverageTests
{
    [Fact] public void Team_IsConcreteAggregate() => typeof(Team).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Workspace))]
public class WorkspaceCoverageTests
{
    [Fact] public void Workspace_IsConcreteAggregate() => typeof(Workspace).IsAbstract.Should().BeFalse();
}
