using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// TAC-AI-FLOW-04 — an n8n dispatch whose rule carries invalid/missing
/// webhook configuration must settle as a terminal prism failure during the
/// prepare phase WITHOUT invoking the Integrations provider port. Retrying the
/// same durable intent cannot repair a deterministic config defect, so it must
/// not enter the technical retry loop.
/// </summary>
public class N8nDispatchUseCaseTests
{
    private static readonly DateTimeOffset TestNow = new(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private sealed class FixedClock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private async Task<(TestAutomationDbContext Context, AutomationExecution Execution, Guid RuleId)> SeedAsync(
        string? actionConfiguration)
    {
        var options = new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var context = new TestAutomationDbContext(options);

        var rule = AutomationRule.Create(
            AccountId, WorkspaceId, "N8N rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemAssigned"),
                AutomationActionDefinition.Create("Webhook", actionConfiguration)),
            OwnerId, TestNow);
        rule.Enable(OwnerId, TestNow);
        var execution = AutomationExecution.Create(AccountId, WorkspaceId, rule.Id, Guid.NewGuid(), TestNow);

        context.AutomationRules.Add(rule);
        context.AutomationExecutions.Add(execution);
        await context.SaveChangesAsync();
        return (context, execution, rule.Id);
    }

    private static N8nDispatchUseCase CreateUseCase(
        IAutomationDbContext context,
        IN8nWebhookActions? provider) =>
        new(context, provider ?? new FakeProvider(), new FixedClock(TestNow));

    private static N8nDispatchRequestedV1 NewMessage(Guid executionId, Guid ruleId) =>
        new(Guid.CreateVersion7(), executionId, ruleId, AccountId, WorkspaceId,
            TestNow, Guid.NewGuid(), SourceEventId: null, CausationId: null);

    [Fact]
    public async Task Prepare_UrlOnlyConfigurationWithoutWebhookPath_SettlesTerminalWithoutProviderCall()
    {
        var (context, execution, ruleId) = await SeedAsync(actionConfiguration: """{"url":"https://example.com/hooks/generic"}""");
        var provider = new RecordingProvider();
        var useCase = CreateUseCase(context, provider);

        var preparation = await useCase.PrepareAttemptAsync(NewMessage(execution.Id, ruleId), CancellationToken.None);
        await context.SaveChangesAsync();

        preparation.Outcome.Should().Be(N8nDispatchPrepareOutcome.PrerequisiteSettled,
            "a rule that is Domain-valid (has 'url') but not n8n-dispatchable (no webhookPath) is a deterministic config defect");
        context.ChangeTracker.Clear();
        var stored = await context.AutomationExecutions.SingleAsync(e => e.Id == execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Failed,
            "the config defect settles the execution as a terminal failure, not a transport retry");
        stored.Error.Should().Contain("webhookPath",
            "the error names the missing configuration member");
        provider.CallCount.Should().Be(0,
            "the provider port must never be invoked for an invalid configuration");
    }

    private sealed class RecordingProvider : IN8nWebhookActions
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
            string webhookPath,
            string payload,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
        }
    }

    private sealed class FakeProvider : IN8nWebhookActions
    {
        public Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
            string webhookPath,
            string payload,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("provider must not be invoked during prepare");
    }
}