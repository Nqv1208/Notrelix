using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Tests.Features.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// Rules are managed by a board's Managers (for that board's rules) and by workspace Owners and
/// Admins; a rule without a board stays workspace settings. The use cases name the board they are
/// authorized on and the handler proves the rule is that board's.
/// </summary>
public sealed class AutomationRuleManagementAuthorityTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Board = Guid.CreateVersion7();
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static readonly AccessPolicyEngine Engine = new();

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Workspace, null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor Descriptor = new(
        typeof(Probe), ApplicationRequestKind.Query, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Write,
        new AccessRequirements(true, false, false, false), false, false);

    public static TheoryData<IRequirePermission> BoardScopedRequests() => new()
    {
        new CreateAutomationRuleCommand(Workspace, "r", "ItemCreated", "MoveItem", """{"targetGroupId":"x"}""", Board),
        new SetAutomationRuleEnabledCommand(Guid.NewGuid(), false, Board),
        new GetWorkspaceAutomationsQuery(Workspace, Board),
        new GetAutomationExecutionsQuery(Guid.NewGuid(), 1, 20, Board),
    };

    [Theory]
    [MemberData(nameof(BoardScopedRequests))]
    public void BoardScoped_RequestsNeedManageBoardOnTheBoard(IRequirePermission request)
    {
        request.Action.Should().Be(PermissionAction.ManageBoard);
        request.Resource!.Kind.Value.Should().Be("work-management.board");
        request.Resource.ResourceId.Should().Be(Board);
        (request as IRequireAdditionalPermission)?.AdditionalActionNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void BoardPathEnable_NeedsManageBoard_AndTheHandlerGuardsIntegrationRules()
    {
        ((IRequirePermission)new SetAutomationRuleEnabledCommand(Guid.NewGuid(), true, Board)).Action
            .Should().Be(PermissionAction.ManageBoard);
        // Without a named board the integration permission is required at the pipeline.
        ((IRequireAdditionalPermission)new SetAutomationRuleEnabledCommand(Guid.NewGuid(), true)).AdditionalActionNames
            .Should().Equal(nameof(PermissionAction.ManageIntegrations));
    }

    [Fact]
    public void WorkspaceWideRules_NeedManageAutomation()
    {
        ((IRequirePermission)new CreateAutomationRuleCommand(Workspace, "r", "ItemCreated", "MoveItem", "{}")).Action
            .Should().Be(PermissionAction.ManageAutomation);
        ((IRequirePermission)new SetAutomationRuleEnabledCommand(Guid.NewGuid(), true)).Action
            .Should().Be(PermissionAction.ManageAutomation);
        ((IRequirePermission)new GetWorkspaceAutomationsQuery(Workspace)).Action
            .Should().Be(PermissionAction.ManageAutomation);
    }

    [Fact]
    public void ManageBoard_AllowsManagersAndOwners_AndDeniesEditorsAndMembers()
    {
        Decide(Facts("Member", audience: "Private", explicitRank: 4)).Should().Be(AccessDecisionKind.Allowed, "a board Manager");
        Decide(Facts("Owner")).Should().Be(AccessDecisionKind.Allowed, "a workspace Owner");
        // A workspace Admin on an unplaced board is an Editor there, so Admins manage rules through
        // the workspace-settings path (no board named); see the next test.
        Decide(Facts("Member", audience: "Private", explicitRank: 3)).Should().Be(AccessDecisionKind.Forbidden, "a board Editor");
        Decide(Facts("Member")).Should().Be(AccessDecisionKind.Forbidden, "a plain member");
        Decide(Facts("Guest", explicitRank: 4)).Should().Be(AccessDecisionKind.Forbidden, "a guest is capped at Editor");
    }

    [Fact]
    public void WorkspaceSettings_StayOwnerAndAdminOnly_ForWorkspaceWideRules()
    {
        var probe = new Probe(PermissionAction.ManageWorkspaceSettings, "automation.rule");
        Engine.Evaluate(Descriptor, Context, Facts("Admin"), probe).Kind.Should().Be(AccessDecisionKind.Allowed);
        Engine.Evaluate(Descriptor, Context, Facts("Member", audience: "Private", explicitRank: 4), probe).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "a board Manager does not manage workspace-wide rules");
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule NewRule(TestAutomationDbContext context, Guid? boardId)
    {
        var rule = AutomationRule.Create(
            Guid.CreateVersion7(), Workspace, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemMovedToGroup", $$"""{"groupId":"{{Guid.CreateVersion7()}}"}"""),
                AutomationActionDefinition.Create("MoveItem", $$"""{"targetGroupId":"{{Guid.CreateVersion7()}}"}""")),
            Guid.CreateVersion7(), T0, boardId);
        context.AutomationRules.Add(rule);
        return rule;
    }

    [Fact]
    public async Task EnableClaimingAnotherBoard_IsNotFound_AndNothingChanges()
    {
        using var context = NewContext();
        var rule = NewRule(context, Board);
        await context.SaveChangesAsync();
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context, Mock.Of<ICurrentUser>(), Mock.Of<IDateTimeProvider>(),
            new AutomationExecutorAuthority(Mock.Of<Notrelix.Application.Features.Automation.Ports.WorkManagement.IWorkActionPort>()));

        var act = () => handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, false, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
        (await context.AutomationRules.AsNoTracking().SingleAsync()).IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task BoardPathEnable_OfAnIntegrationRule_IsForbidden_EvenForTheRulesOwnBoard()
    {
        using var context = NewContext();
        var rule = AutomationRule.Create(
            Guid.CreateVersion7(), Workspace, "hook",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated", null),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"hook"}""")),
            Guid.CreateVersion7(), T0, Board);
        context.AutomationRules.Add(rule);
        await context.SaveChangesAsync();
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context, Mock.Of<ICurrentUser>(), Mock.Of<IDateTimeProvider>(),
            new AutomationExecutorAuthority(Mock.Of<Notrelix.Application.Features.Automation.Ports.WorkManagement.IWorkActionPort>()));

        var act = () => handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true, Board), default);

        await act.Should().ThrowAsync<ForbiddenException>();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableClaimingTheRulesBoard_Proceeds()
    {
        using var context = NewContext();
        var rule = NewRule(context, Board);
        await context.SaveChangesAsync();
        var user = Mock.Of<ICurrentUser>(u => u.UserId == (rule.ExecutorUserId ?? Guid.CreateVersion7()));
        var clock = Mock.Of<IDateTimeProvider>(c => c.UtcNow == T0);
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context, user, clock,
            new AutomationExecutorAuthority(Mock.Of<Notrelix.Application.Features.Automation.Ports.WorkManagement.IWorkActionPort>()));

        var result = await handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true, Board), default);

        result.Succeeded.Should().BeTrue();
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecutionHistoryClaimingAnotherBoard_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context, Board);
        var workspaceWide = NewRule(context, null);
        await context.SaveChangesAsync();
        var handler = new GetAutomationExecutionsQueryHandler(context);

        var otherBoard = () => handler.Handle(new GetAutomationExecutionsQuery(rule.Id, 1, 20, Guid.CreateVersion7()), default);
        var wideRule = () => handler.Handle(new GetAutomationExecutionsQuery(workspaceWide.Id, 1, 20, Board), default);
        var own = await handler.Handle(new GetAutomationExecutionsQuery(rule.Id, 1, 20, Board), default);

        await otherBoard.Should().ThrowAsync<NotFoundException>();
        await wideRule.Should().ThrowAsync<NotFoundException>("authority on a board never reaches a workspace-wide rule");
        own.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task BoardListShowsOnlyThatBoardsRules()
    {
        using var context = NewContext();
        var mine = NewRule(context, Board);
        NewRule(context, Guid.CreateVersion7());
        NewRule(context, null);
        await context.SaveChangesAsync();
        var handler = new GetWorkspaceAutomationsQueryHandler(context, Mock.Of<ICurrentUser>());

        var result = await handler.Handle(new GetWorkspaceAutomationsQuery(Workspace, Board), default);

        result.Data!.Select(rule => rule.Id).Should().Equal(mine.Id);
    }

    private static AccessDecisionKind Decide(AccessFacts facts) =>
        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.ManageBoard, "work-management.board")).Kind;

    private static AccessFacts Facts(string role, string audience = "Workspace", int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null);

    private sealed record Probe(PermissionAction Action, string Kind) : IRequirePermission
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.NewGuid());
    }
}
