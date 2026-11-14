using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Tests.Features.Automation.Executions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// UC-AUT-D10: enabling a board rule checks every action and the trigger's group, and refuses with a
/// structured error that lists each archived or deleted target (kind, id, field), not only the first.
/// </summary>
public sealed class AutomationBrokenReferenceTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Board = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static readonly Guid TriggerGroup = Guid.CreateVersion7();
    private static readonly Guid DeletedOption = Guid.CreateVersion7();
    private static readonly Guid LiveOption = Guid.CreateVersion7();
    private static readonly Guid ArchivedAssignee = Guid.CreateVersion7();

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static Mock<IWorkActionPort> Port()
    {
        var port = new Mock<IWorkActionPort>();
        // The trigger group, and the second action's status option, are gone; everything else is reachable.
        port.Setup(p => p.EnsureBoardGroupAsync(Board, TriggerGroup, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("group"));
        port.Setup(p => p.EnsureStatusOptionAsync(Board, DeletedOption, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("option"));
        port.Setup(p => p.EnsureWorkspaceMemberAsync(ArchivedAssignee, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("member"));
        return port;
    }

    private static AutomationRule NewBoardRule(TestAutomationDbContext context)
    {
        var configuration = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated", $$"""{"groupId":"{{TriggerGroup}}"}"""),
            AutomationActionDefinition.Create("ChangeStatus", $$"""{"optionId":"{{LiveOption}}"}"""),
            moreActions: [
                AutomationActionDefinition.Create("ChangeStatus", $$"""{"optionId":"{{DeletedOption}}"}"""),
                AutomationActionDefinition.Create("AssignPerson", $$"""{"userId":"{{ArchivedAssignee}}"}"""),
            ]);
        var rule = AutomationRule.Create(Guid.CreateVersion7(), Workspace, "rule", configuration, Actor, T0, Board);
        context.AutomationRules.Add(rule);
        return rule;
    }

    [Fact]
    public async Task EnableWithSeveralBrokenTargets_RefusesAndListsEachOne()
    {
        using var context = NewContext();
        var rule = NewBoardRule(context);
        await context.SaveChangesAsync();
        var capacity = new Mock<IBillingCapacityActions>();
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context,
            Mock.Of<ICurrentUser>(u => u.UserId == Actor),
            Mock.Of<IDateTimeProvider>(c => c.UtcNow == T0),
            new AutomationExecutorAuthority(Port().Object),
            capacity.Object);

        var act = () => handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true, Board), default);

        var thrown = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        thrown.Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Rule_BrokenReference);
        var references = (IEnumerable<Dictionary<string, object?>>)thrown.Which.Details["brokenReferences"]!;
        references.Select(r => (r["kind"], r["id"], r["field"])).Should().BeEquivalentTo(new[]
        {
            ((object?)"option", (object?)DeletedOption, (object?)"actions[1]"),
            ((object?)"member", (object?)ArchivedAssignee, (object?)"actions[2]"),
            ((object?)"group", (object?)TriggerGroup, (object?)"trigger.groupId"),
        });
        rule.IsEnabled.Should().BeFalse("a refused enable changes nothing");
        capacity.Verify(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindBrokenReferences_WhenEverythingIsReachable_ReturnsNone()
    {
        var port = new Mock<IWorkActionPort>();
        var sut = new AutomationExecutorAuthority(port.Object);
        var configuration = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated", $$"""{"groupId":"{{TriggerGroup}}"}"""),
            AutomationActionDefinition.Create("ChangeStatus", $$"""{"optionId":"{{LiveOption}}"}"""));

        var broken = await sut.FindBrokenReferencesAsync(
            Guid.CreateVersion7(), Workspace, Actor, configuration, Board, configuration.Trigger.Configuration, default);

        broken.Should().BeEmpty();
    }
}
