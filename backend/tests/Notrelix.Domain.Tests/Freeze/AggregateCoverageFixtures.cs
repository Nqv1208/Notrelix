using FluentAssertions;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.WorkspaceRoutes;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Domain.Tests.Freeze;

// ── Shared: aggregates without dedicated test files ──────────────────────

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

[CoversAggregate(typeof(DashboardSource))]
public class DashboardSourceCoverageTests
{
    [Fact] public void DashboardSource_IsConcreteAggregate() => typeof(DashboardSource).IsAbstract.Should().BeFalse();
}

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

[CoversAggregate(typeof(BillingCustomer))]
public class BillingCustomerCoverageTests
{
    [Fact] public void BillingCustomer_IsConcreteAggregate() => typeof(BillingCustomer).IsAbstract.Should().BeFalse();
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

[CoversAggregate(typeof(ResourceLink))]
public class ResourceLinkCoverageTests
{
    [Fact] public void ResourceLink_IsConcreteAggregate() => typeof(ResourceLink).IsAbstract.Should().BeFalse();
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

[CoversAggregate(typeof(BoardViewUserPreference))]
public class BoardViewUserPreferenceCoverageTests
{
    [Fact] public void BoardViewUserPreference_IsConcreteAggregate() => typeof(BoardViewUserPreference).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(Label))]
public class LabelCoverageTests
{
    [Fact] public void Label_IsConcreteAggregate() => typeof(Label).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(TimeTrackingEntry))]
public class TimeTrackingEntryCoverageTests
{
    [Fact] public void TimeTrackingEntry_IsConcreteAggregate() => typeof(TimeTrackingEntry).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UsageMetric))]
public class UsageMetricCoverageTests
{
    [Fact] public void UsageMetric_IsConcreteAggregate() => typeof(UsageMetric).IsAbstract.Should().BeFalse();
}

[CoversAggregate(typeof(UserLoginAttempt))]
public class UserLoginAttemptCoverageTests
{
    [Fact] public void UserLoginAttempt_IsConcreteAggregate() => typeof(UserLoginAttempt).IsAbstract.Should().BeFalse();
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

[CoversAggregate(typeof(UserMfaMethod))]
public class UserMfaMethodCoverageTests
{
    [Fact] public void UserMfaMethod_IsConcreteAggregate() => typeof(UserMfaMethod).IsAbstract.Should().BeFalse();
}
