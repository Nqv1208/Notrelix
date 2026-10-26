using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Plans.Commands.ChangeAccountPlan;
using Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Tests.Features.Billing;

public class BillingPlanTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly Mock<IBillingDbContext> _context = new();
    private readonly Mock<IBillingUsageReader> _reader = new();
    private readonly Mock<IBillingPlanChangePolicy> _policy = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly List<Subscription> _subscriptions = [];

    public BillingPlanTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _requestContext.Setup(c => c.RequireAccountId()).Returns(AccountId);
        _requestContext.Setup(c => c.UserId).Returns(UserId);
        _policy.Setup(p => p.IsPlanChangeEnabled).Returns(true);
        _reader.Setup(r => r.IsAccountOwnerAsync(AccountId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var subscriptions = TestBillingDbSet.Create(_subscriptions);
        _context.Setup(c => c.Subscriptions).Returns(subscriptions.Object);
    }

    private ChangeAccountPlanCommandHandler ChangeHandler() =>
        new(_context.Object, _reader.Object, _policy.Object, _requestContext.Object, _clock.Object);

    private GetWorkspaceBillingPlanQueryHandler QueryHandler() =>
        new(new BillingPlanResolver(_context.Object, _clock.Object), _reader.Object, _policy.Object, _requestContext.Object);

    [Fact]
    public void The_catalog_defines_the_three_plans_with_their_limits()
    {
        Assert.Equal(["free", "pro", "enterprise"], BillingPlanCatalog.All.Select(p => p.Code));

        Assert.Equal(5, BillingPlanCatalog.Free.Limits[BillingCapabilityCode.AutomationRule]);
        Assert.Equal(10, BillingPlanCatalog.Free.Limits[BillingCapabilityCode.MemberSeat]);
        Assert.Equal(2, BillingPlanCatalog.Free.Limits[BillingCapabilityCode.StorageGb]);
        Assert.Equal(5, BillingPlanCatalog.Free.Limits[BillingCapabilityCode.GuestSeat]);

        Assert.Equal(100, BillingPlanCatalog.Pro.Limits[BillingCapabilityCode.AutomationRule]);
        Assert.Equal(500, BillingPlanCatalog.Pro.Limits[BillingCapabilityCode.MemberSeat]);
        Assert.Equal(100, BillingPlanCatalog.Pro.Limits[BillingCapabilityCode.StorageGb]);
        Assert.Equal(200, BillingPlanCatalog.Pro.Limits[BillingCapabilityCode.GuestSeat]);

        Assert.Null(BillingPlanCatalog.Enterprise.Limits[BillingCapabilityCode.AutomationRule]);
        Assert.Null(BillingPlanCatalog.Enterprise.Limits[BillingCapabilityCode.MemberSeat]);
        Assert.Equal(1000, BillingPlanCatalog.Enterprise.Limits[BillingCapabilityCode.StorageGb]);
        Assert.Null(BillingPlanCatalog.Enterprise.Limits[BillingCapabilityCode.GuestSeat]);
    }

    [Fact]
    public async Task An_account_without_a_subscription_is_on_the_free_plan()
    {
        var result = await QueryHandler().Handle(new GetWorkspaceBillingPlanQuery(WorkspaceId), CancellationToken.None);

        Assert.Equal("free", result.Data!.CurrentPlan.Code);
        Assert.Equal(3, result.Data.AvailablePlans.Count);
        Assert.True(result.Data.CanChangePlan);
    }

    [Fact]
    public async Task A_non_owner_or_a_locked_environment_cannot_change_the_plan()
    {
        _reader.Setup(r => r.IsAccountOwnerAsync(AccountId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var notOwner = await QueryHandler().Handle(new GetWorkspaceBillingPlanQuery(WorkspaceId), CancellationToken.None);
        Assert.False(notOwner.Data!.CanChangePlan);

        _reader.Setup(r => r.IsAccountOwnerAsync(AccountId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _policy.Setup(p => p.IsPlanChangeEnabled).Returns(false);
        var locked = await QueryHandler().Handle(new GetWorkspaceBillingPlanQuery(WorkspaceId), CancellationToken.None);
        Assert.False(locked.Data!.CanChangePlan);
    }

    [Fact]
    public async Task Changing_the_plan_starts_a_subscription_when_the_account_has_none()
    {
        var result = await ChangeHandler().Handle(new ChangeAccountPlanCommand(WorkspaceId, "pro"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("pro", result.Data!.CurrentPlan.Code);
        _context.Verify(c => c.Subscriptions.Add(It.Is<Subscription>(s =>
            s.AccountId == AccountId && s.Tier == SubscriptionTier.Pro && s.PlanId == BillingPlanCatalog.Pro.PlanId)), Times.Once);
    }

    [Fact]
    public async Task Changing_the_plan_moves_an_existing_subscription_to_the_new_tier()
    {
        var free = BillingPlanCatalog.Free;
        var existing = Subscription.Create(AccountId, free.PlanId, free.Tier, Now.AddDays(-1), Now.AddYears(1), null, Now.AddDays(-1));
        _subscriptions.Add(existing);

        var result = await ChangeHandler().Handle(new ChangeAccountPlanCommand(WorkspaceId, "Enterprise"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(SubscriptionTier.Enterprise, existing.Tier);
        Assert.Equal(BillingPlanCatalog.Enterprise.PlanId, existing.PlanId);
    }

    [Fact]
    public async Task Changing_the_plan_is_refused_when_the_environment_does_not_allow_it()
    {
        _policy.Setup(p => p.IsPlanChangeEnabled).Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            ChangeHandler().Handle(new ChangeAccountPlanCommand(WorkspaceId, "pro"), CancellationToken.None));
    }

    [Fact]
    public async Task Changing_the_plan_needs_the_account_owner()
    {
        _reader.Setup(r => r.IsAccountOwnerAsync(AccountId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            ChangeHandler().Handle(new ChangeAccountPlanCommand(WorkspaceId, "pro"), CancellationToken.None));
    }

    [Fact]
    public async Task An_unknown_plan_code_is_rejected()
    {
        var result = await ChangeHandler().Handle(new ChangeAccountPlanCommand(WorkspaceId, "platinum"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public async Task A_seat_is_admitted_when_the_person_already_holds_it_or_one_is_free(
        bool alreadySeated, bool freeSeat, bool expected)
    {
        var facts = new Mock<IBillingCapabilityFacts>();
        facts.Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.MemberSeat, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(freeSeat, 10, freeSeat ? 3 : 10, freeSeat ? 7 : 0));
        _reader.Setup(r => r.HasSeatAsync(AccountId, WorkspaceId, UserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadySeated);

        var gate = new BillingSeatGate(facts.Object, _reader.Object);

        Assert.Equal(expected, await gate.CanAdmitAsync(AccountId, WorkspaceId, UserId, asGuest: false, CancellationToken.None));
    }

    [Fact]
    public async Task A_guest_seat_is_checked_against_the_guest_capability()
    {
        var facts = new Mock<IBillingCapabilityFacts>();
        facts.Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.GuestSeat, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(false, 5, 5, 0));

        var gate = new BillingSeatGate(facts.Object, _reader.Object);

        Assert.False(await gate.CanAdmitAsync(AccountId, WorkspaceId, UserId, asGuest: true, CancellationToken.None));
    }
}
