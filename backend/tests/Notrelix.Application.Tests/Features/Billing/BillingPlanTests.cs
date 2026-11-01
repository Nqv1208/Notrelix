using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Plans;
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
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<Plan> _plans = TestBaselinePlans.All();

    public BillingPlanTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _requestContext.Setup(c => c.RequireAccountId()).Returns(AccountId);
        _requestContext.Setup(c => c.UserId).Returns(UserId);
        _context.Setup(c => c.Subscriptions).Returns(TestBillingDbSet.Create(_subscriptions).Object);
        _context.Setup(c => c.Plans).Returns(TestBillingDbSet.Create(_plans).Object);
    }

    private BillingPlanCatalogReader Catalog() => new(_context.Object);

    private BillingPlanResolver Resolver() => new(_context.Object, _clock.Object, Catalog());

    private GetWorkspaceBillingPlanQueryHandler QueryHandler() => new(Resolver(), Catalog(), _requestContext.Object);

    private Subscription Subscribe(Plan plan) =>
        Subscription.Create(AccountId, plan.Id, plan.Tier, Now.AddDays(-1), Now.AddYears(1), null, Now.AddDays(-1));

    [Fact]
    public async Task The_catalog_is_read_from_the_plan_rows_with_their_limits()
    {
        var plans = await Catalog().ListSellableAsync(CancellationToken.None);

        Assert.Equal(["free", "pro", "enterprise"], plans.Select(p => p.Code));
        var free = plans.Single(p => p.Code == "free");
        Assert.Equal(5, free.Limits[BillingCapabilityCode.AutomationRule]);
        Assert.Equal(10, free.Limits[BillingCapabilityCode.MemberSeat]);
        Assert.Equal(2, free.Limits[BillingCapabilityCode.StorageGb]);
        Assert.Equal(5, free.Limits[BillingCapabilityCode.GuestSeat]);
        var enterprise = plans.Single(p => p.Code == "enterprise");
        Assert.Null(enterprise.Limits[BillingCapabilityCode.MemberSeat]);
        Assert.Equal(1000, enterprise.Limits[BillingCapabilityCode.StorageGb]);
    }

    [Fact]
    public async Task Only_the_newest_active_revision_of_a_family_is_on_sale()
    {
        var older = TestBaselinePlans.Build("pro", "Pro", SubscriptionTier.Pro, 1, (BillingCapabilityCode.MemberSeat, 1));
        var newer = TestBaselinePlans.Build("pro", "Pro v2", SubscriptionTier.Pro, 2, (BillingCapabilityCode.MemberSeat, 900));
        var retired = TestBaselinePlans.Build("pro", "Pro v3", SubscriptionTier.Pro, 3, (BillingCapabilityCode.MemberSeat, 5));
        retired.Deprecate(Now);
        _plans.AddRange([older, newer, retired]);

        var pro = (await Catalog().ListSellableAsync(CancellationToken.None)).Single(p => p.Code == "pro");

        Assert.Equal(2, pro.Revision);
        Assert.Equal(900, pro.Limits[BillingCapabilityCode.MemberSeat]);
    }

    [Fact]
    public async Task An_account_without_a_subscription_is_on_the_free_plan_and_cannot_change_it_from_a_tenant_session()
    {
        var result = await QueryHandler().Handle(new GetWorkspaceBillingPlanQuery(WorkspaceId), CancellationToken.None);

        Assert.Equal("free", result.Data!.CurrentPlan.Code);
        Assert.Equal(3, result.Data.AvailablePlans.Count);
        Assert.DoesNotContain(typeof(WorkspaceBillingPlanDto).GetProperties(), p => p.Name.Contains("Change", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_subscription_keeps_the_revision_it_references_when_a_newer_one_is_published()
    {
        var pro1 = TestBaselinePlans.Pro;
        var pro2 = TestBaselinePlans.Build("pro", "Pro v2", SubscriptionTier.Pro, 2, (BillingCapabilityCode.MemberSeat, 900));
        _plans.Add(pro2);
        _subscriptions.Add(Subscribe(pro1));

        var plan = await Resolver().ResolveAsync(AccountId, CancellationToken.None);

        Assert.Equal(pro1.Id, plan.PlanId);
        Assert.Equal(500, plan.Limits[BillingCapabilityCode.MemberSeat]);
    }

    [Fact]
    public async Task A_subscription_on_an_archived_revision_still_resolves_its_limits()
    {
        var archived = TestBaselinePlans.Build("pro", "Pro old", SubscriptionTier.Pro, 2, (BillingCapabilityCode.MemberSeat, 42));
        archived.Archive(Now);
        _plans.Add(archived);
        _subscriptions.Add(Subscribe(archived));

        var plan = await Resolver().ResolveAsync(AccountId, CancellationToken.None);

        Assert.Equal(42, plan.Limits[BillingCapabilityCode.MemberSeat]);
    }

    [Fact]
    public async Task A_canceled_subscription_puts_the_account_back_on_the_free_plan()
    {
        var subscription = Subscribe(TestBaselinePlans.Enterprise);
        subscription.CancelImmediately(UserId, Now);
        _subscriptions.Add(subscription);

        Assert.Equal("free", (await Resolver().ResolveAsync(AccountId, CancellationToken.None)).Code);
    }

    [Fact]
    public async Task A_subscription_to_a_plan_the_catalog_no_longer_holds_falls_back_to_the_default_plan()
    {
        _subscriptions.Add(Subscription.Create(AccountId, Guid.CreateVersion7(), SubscriptionTier.Pro, Now.AddDays(-1), Now.AddYears(1), null, Now.AddDays(-1)));

        Assert.Equal("free", (await Resolver().ResolveAsync(AccountId, CancellationToken.None)).Code);
    }

    [Fact]
    public async Task A_catalog_without_an_active_default_plan_is_a_fault_not_a_guess()
    {
        _plans.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Resolver().ResolveAsync(AccountId, CancellationToken.None));
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
