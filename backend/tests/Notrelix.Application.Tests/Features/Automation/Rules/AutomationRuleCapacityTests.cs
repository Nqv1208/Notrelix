using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Commands.DeleteAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Tests.Features.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// UC-AUT-M04: only enabled rules count toward the plan limit. Enable consumes one slot, disable
/// and delete release the slot of a rule that holds one, a Draft or manually Disabled rule holds none,
/// and a platform auto-disable keeps the slot until a person acts on the rule.
/// </summary>
public sealed class AutomationRuleCapacityTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule NewRule(TestAutomationDbContext context)
    {
        var rule = AutomationRule.Create(
            Guid.CreateVersion7(), Workspace, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated", "{}"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"some-hook"}""")),
            Actor, T0, boardId: null);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static SetAutomationRuleEnabledCommandHandler Enabler(TestAutomationDbContext context, Mock<IBillingCapacityActions> capacity) =>
        new(context,
            Mock.Of<ICurrentUser>(u => u.UserId == Actor),
            Mock.Of<IDateTimeProvider>(c => c.UtcNow == T0),
            new AutomationExecutorAuthority(Mock.Of<IWorkActionPort>()),
            capacity.Object);

    private static DeleteAutomationRuleCommandHandler Deleter(TestAutomationDbContext context, Mock<IBillingCapacityActions> capacity) =>
        new(context,
            Mock.Of<ICurrentRequestContext>(r => r.UserId == Actor),
            Mock.Of<IDateTimeProvider>(c => c.UtcNow == T0),
            capacity.Object);

    private static Mock<IBillingCapacityActions> Capacity()
    {
        var capacity = new Mock<IBillingCapacityActions>();
        capacity.Setup(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumeCapacityResult(AlreadyConsumed: false, Remaining: null));
        capacity.Setup(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReleaseCapacityResult(AlreadyReleased: false, Remaining: null));
        return capacity;
    }

    [Fact]
    public async Task EnableDraft_ConsumesOneSlot()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();

        await Enabler(context, capacity).Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        capacity.Verify(c => c.ConsumeAsync(
            It.Is<ConsumeCapacityRequest>(r => r.Operation.CapabilityCode == Notrelix.Application.Features.Billing.Public.Facts.BillingCapabilityCode.AutomationRule
                && r.Operation.Amount == 1 && r.Operation.SourceResource == rule.Id.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableAlreadyEnabled_ConsumesNothingMore()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        var handler = Enabler(context, capacity);

        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        capacity.Verify(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableEnabledRule_ReleasesItsSlot()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        var handler = Enabler(context, capacity);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, false), default);

        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableDraft_ReleasesNothing()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();

        await Enabler(context, capacity).Handle(new SetAutomationRuleEnabledCommand(rule.Id, false), default);

        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableAfterDisable_ConsumesAgainUnderADistinctOperation()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        var handler = Enabler(context, capacity);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, false), default);

        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        var operations = new List<Guid>();
        capacity.Invocations
            .Where(i => i.Method.Name == nameof(IBillingCapacityActions.ConsumeAsync))
            .ToList()
            .ForEach(i => operations.Add(((ConsumeCapacityRequest)i.Arguments[0]).Operation.LogicalOperationId));
        operations.Should().HaveCount(2);
        operations.Distinct().Should().HaveCount(2, "a new enable cycle is a new capacity effect");
    }

    [Fact]
    public async Task DeleteEnabledRule_ReleasesItsSlot()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        await Enabler(context, capacity).Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        await Deleter(context, capacity).Handle(new DeleteAutomationRuleCommand(rule.Id), default);

        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDraft_ReleasesNothing()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();

        await Deleter(context, capacity).Handle(new DeleteAutomationRuleCommand(rule.Id), default);

        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableOfAutoDisabledRule_KeepsItsSlotAndConsumesNothing()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        var handler = Enabler(context, capacity);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);
        rule.DisableAutomatically("executor lost access", T0);
        await context.SaveChangesAsync();

        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);

        capacity.Verify(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableAutoDisabledRule_ReleasesTheSlotItStillHolds()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();
        var capacity = Capacity();
        var handler = Enabler(context, capacity);
        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), default);
        rule.DisableAutomatically("executor lost access", T0);
        await context.SaveChangesAsync();

        await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, false), default);

        capacity.Verify(c => c.ReleaseAsync(It.IsAny<ReleaseCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
