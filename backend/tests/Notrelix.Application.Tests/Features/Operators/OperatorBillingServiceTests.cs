using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Operator;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Tests.Features.Billing;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Common;

namespace Notrelix.Application.Tests.Features.Operators;

public class OperatorBillingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();

    private readonly List<Plan> _plans = TestBaselinePlans.All();
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<Entitlement> _entitlements = [];
    private readonly Mock<IBillingDbContext> _context = new();
    private readonly Mock<IAccountMembershipFacts> _accounts = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly RecordingAudit _audit = new();
    private readonly CountingCommit _commit = new();

    public OperatorBillingServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Plans).Returns(TestBillingDbSet.Create(_plans).Object);
        _context.Setup(c => c.Subscriptions).Returns(TestBillingDbSet.Create(_subscriptions).Object);
        _context.Setup(c => c.Entitlements).Returns(TestBillingDbSet.Create(_entitlements).Object);
        _accounts.Setup(a => a.GetAdmissionAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountMembershipAdmissionFact(true));
    }

    private OperatorPlanCatalogService Catalog() => new(_context.Object, _audit, _commit, _clock.Object);

    private OperatorAccountBillingService Billing() => new(
        _context.Object, new BillingPlanResolver(_context.Object, _clock.Object, new BillingPlanCatalogReader(_context.Object)),
        _accounts.Object, _audit, _commit, new FakeOperator(), _clock.Object);

    private static OperatorPlanRevisionInput Revision(int revision, string family = "pro", string name = "Pro", params OperatorPlanLimitDto[] limits) =>
        new(family, revision, name, "Pro", 99000m, "VND", "Monthly",
            limits.Length == 0 ? [new OperatorPlanLimitDto(BillingCapabilityCode.MemberSeat, false, 800)] : limits, "price review");

    private Subscription Subscribe(Plan plan)
    {
        var subscription = Subscription.Create(AccountId, plan.Id, plan.Tier, Now.AddDays(-1), Now.AddYears(1), null, Now.AddDays(-1));
        ((IHasDomainEvents)subscription).ClearDomainEvents();
        _subscriptions.Add(subscription);
        return subscription;
    }

    // ── UC-BIL-A03: the catalog by revision ────────────────────────────────

    [Fact]
    public async Task A_new_revision_is_published_with_its_limits_and_audited()
    {
        var created = await Catalog().CreateRevisionAsync(Revision(2), CancellationToken.None);

        Assert.Equal(("pro", 2, "Active"), (created.FamilyKey, created.Revision, created.Status));
        Assert.Equal(800, Assert.Single(created.Limits).Limit);
        Assert.Equal("plan.create-revision", Assert.Single(_audit.Records).Action);
        Assert.Equal(1, _commit.Commits);
    }

    [Fact]
    public async Task Re_submitting_the_same_revision_with_the_same_content_is_a_no_op()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Catalog().CreateRevisionAsync(Revision(1, "pro", "Pro"), CancellationToken.None));
        var pro = TestBaselinePlans.Pro;
        var same = new OperatorPlanRevisionInput("pro", 1, "Pro", "Pro", 0m, "VND", "Monthly",
            pro.Limits.Select(l => new OperatorPlanLimitDto(l.Feature.Code, l.IsUnlimited, l.Limit)).ToList(), "again");

        var result = await Catalog().CreateRevisionAsync(same, CancellationToken.None);

        Assert.Equal(pro.Id, result.Id);
        Assert.Empty(_audit.Records);
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task A_revision_number_that_skips_ahead_is_a_conflict()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Catalog().CreateRevisionAsync(Revision(5), CancellationToken.None));
        Assert.Contains("next revision", ex.Message);
    }

    [Fact]
    public async Task Two_limits_for_one_feature_and_an_unknown_feature_are_rejected()
    {
        var twice = Revision(2, "pro", "Pro",
            new OperatorPlanLimitDto(BillingCapabilityCode.MemberSeat, false, 1), new OperatorPlanLimitDto(BillingCapabilityCode.MemberSeat, false, 2));
        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() => Catalog().CreateRevisionAsync(twice, CancellationToken.None));

        var unknown = Revision(2, "pro", "Pro", new OperatorPlanLimitDto("TELEPORTS", false, 1));
        await Assert.ThrowsAsync<ValidationException>(() => Catalog().CreateRevisionAsync(unknown, CancellationToken.None));
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task A_bad_family_key_a_negative_limit_and_a_negative_price_are_rejected()
    {
        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() => Catalog().CreateRevisionAsync(Revision(1, "Bad Key!"), CancellationToken.None));
        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() => Catalog().CreateRevisionAsync(
            Revision(1, "team", "Team", new OperatorPlanLimitDto(BillingCapabilityCode.MemberSeat, false, -1)), CancellationToken.None));
        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() => Catalog().CreateRevisionAsync(
            Revision(1, "team", "Team") with { PriceAmount = -1m }, CancellationToken.None));
    }

    [Fact]
    public async Task Deprecating_stops_sales_but_the_subscription_keeps_resolving_its_limits()
    {
        var pro = TestBaselinePlans.Pro;
        Subscribe(pro);
        var current = _plans.Single(p => p.Id == pro.Id);

        var result = await Catalog().DeprecateAsync(pro.Id, "superseded", current.Version, CancellationToken.None);

        Assert.Equal("Deprecated", result.Status);
        var catalog = new BillingPlanCatalogReader(_context.Object);
        Assert.DoesNotContain(await catalog.ListSellableAsync(CancellationToken.None), p => p.PlanId == pro.Id);
        var resolved = await new BillingPlanResolver(_context.Object, _clock.Object, catalog).ResolveAsync(AccountId, CancellationToken.None);
        Assert.Equal(500, resolved.Limits[BillingCapabilityCode.MemberSeat]);
    }

    [Fact]
    public async Task Deprecating_twice_is_a_no_op_and_a_stale_version_is_a_conflict()
    {
        var pro = TestBaselinePlans.Build("team", "Team", SubscriptionTier.Pro, 1, (BillingCapabilityCode.MemberSeat, 5));
        _plans.Add(pro);

        await Assert.ThrowsAsync<ConflictException>(() => Catalog().DeprecateAsync(pro.Id, "x", pro.Version + 9, CancellationToken.None));
        await Catalog().DeprecateAsync(pro.Id, "first", pro.Version, CancellationToken.None);
        var commits = _commit.Commits;
        await Catalog().DeprecateAsync(pro.Id, "second", pro.Version, CancellationToken.None);

        Assert.Equal(commits, _commit.Commits);
    }

    [Fact]
    public async Task The_last_active_revision_of_the_default_family_cannot_be_retired()
    {
        var free = _plans.Single(p => p.FamilyKey == "free");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => Catalog().ArchiveAsync(free.Id, "x", free.Version, CancellationToken.None));

        Assert.Contains("default plan", ex.Message);
        Assert.Equal(PlanStatus.Active, free.Status);
    }

    [Fact]
    public async Task An_archived_revision_cannot_be_deprecated_and_an_unknown_plan_is_not_found()
    {
        var plan = TestBaselinePlans.Build("team", "Team", SubscriptionTier.Pro, 1);
        plan.Archive(Now);
        _plans.Add(plan);

        await Assert.ThrowsAsync<ConflictException>(() => Catalog().DeprecateAsync(plan.Id, "x", plan.Version, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => Catalog().ArchiveAsync(Guid.CreateVersion7(), "x", 1, CancellationToken.None));
    }

    // ── Plan change (UC-BIL, operator replaces the dev switch) ──────────────

    [Fact]
    public async Task Changing_the_plan_of_an_account_without_a_subscription_starts_one_on_the_chosen_revision()
    {
        await Billing().ChangePlanAsync(AccountId, TestBaselinePlans.Enterprise.Id, "contract signed", null, CancellationToken.None);

        Assert.Equal("subscription.change-plan", Assert.Single(_audit.Records).Action);
        Assert.Equal(1, _commit.Commits);
        _context.Verify(c => c.Subscriptions.Add(It.Is<Subscription>(s =>
            s.AccountId == AccountId && s.PlanId == TestBaselinePlans.Enterprise.Id && s.Tier == SubscriptionTier.Enterprise)), Times.Once);
    }

    [Fact]
    public async Task Changing_the_plan_of_a_subscribed_account_moves_the_subscription_and_audits_before_and_after()
    {
        var subscription = Subscribe(TestBaselinePlans.Free);

        await Billing().ChangePlanAsync(AccountId, TestBaselinePlans.Pro.Id, "upgrade", subscription.Version, CancellationToken.None);

        Assert.Equal((TestBaselinePlans.Pro.Id, SubscriptionTier.Pro), (subscription.PlanId, subscription.Tier));
        var record = Assert.Single(_audit.Records);
        Assert.Contains(TestBaselinePlans.Free.Id.ToString(), System.Text.Json.JsonSerializer.Serialize(record.Before));
        Assert.Contains(TestBaselinePlans.Pro.Id.ToString(), System.Text.Json.JsonSerializer.Serialize(record.After));
    }

    [Fact]
    public async Task Changing_to_the_plan_the_account_is_already_on_is_a_no_op()
    {
        var subscription = Subscribe(TestBaselinePlans.Pro);

        await Billing().ChangePlanAsync(AccountId, TestBaselinePlans.Pro.Id, "same", subscription.Version, CancellationToken.None);

        Assert.Empty(_audit.Records);
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task A_stale_subscription_version_a_retired_plan_and_an_unknown_account_are_refused()
    {
        var subscription = Subscribe(TestBaselinePlans.Free);
        await Assert.ThrowsAsync<ConflictException>(() =>
            Billing().ChangePlanAsync(AccountId, TestBaselinePlans.Pro.Id, "x", subscription.Version + 4, CancellationToken.None));

        var retired = TestBaselinePlans.Build("team", "Team", SubscriptionTier.Pro, 1);
        retired.Deprecate(Now);
        _plans.Add(retired);
        await Assert.ThrowsAsync<ConflictException>(() => Billing().ChangePlanAsync(AccountId, retired.Id, "x", null, CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Billing().ChangePlanAsync(Guid.CreateVersion7(), TestBaselinePlans.Pro.Id, "x", null, CancellationToken.None));
        Assert.Equal(0, _commit.Commits);
    }

    // ── UC-BIL-R09: terminate now ───────────────────────────────────────────

    [Fact]
    public async Task Terminating_cancels_the_subscription_now_audits_it_and_puts_the_account_on_free()
    {
        var subscription = Subscribe(TestBaselinePlans.Enterprise);

        var result = await Billing().TerminateSubscriptionAsync(AccountId, "fraud", subscription.Version, CancellationToken.None);

        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
        Assert.Equal("subscription.terminate", Assert.Single(_audit.Records).Action);
        Assert.Equal("free", result.CurrentPlan.FamilyKey);
    }

    [Fact]
    public async Task Terminating_twice_is_a_no_op_a_stale_version_conflicts_and_no_subscription_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Billing().TerminateSubscriptionAsync(AccountId, "x", 1, CancellationToken.None));

        var subscription = Subscribe(TestBaselinePlans.Pro);
        await Assert.ThrowsAsync<ConflictException>(() => Billing().TerminateSubscriptionAsync(AccountId, "x", subscription.Version + 3, CancellationToken.None));
        await Billing().TerminateSubscriptionAsync(AccountId, "first", subscription.Version, CancellationToken.None);
        var commits = _commit.Commits;

        await Billing().TerminateSubscriptionAsync(AccountId, "second", subscription.Version, CancellationToken.None);

        Assert.Equal(commits, _commit.Commits);
    }

    // ── UC-BIL-D02 / D03: manual override and its revoke or expiry ──────────

    [Fact]
    public async Task Granting_an_override_creates_a_manual_entitlement_with_expiry_and_audits_it()
    {
        var expires = Now.AddDays(30);

        var result = await Billing().GrantOverrideAsync(
            AccountId, new OperatorOverrideInput(BillingCapabilityCode.MemberSeat, false, 2000, expires, "pilot customer"), CancellationToken.None);

        var entitlement = _context.Invocations.Count > 0 ? _entitlements : _entitlements;
        _context.Verify(c => c.Entitlements.Add(It.Is<Entitlement>(e =>
            e.Source == EntitlementSource.Manual && e.Limit == 2000 && e.ExpiresAt == expires && e.AccountId == AccountId)), Times.Once);
        Assert.Equal("entitlement.grant", Assert.Single(_audit.Records).Action);
        Assert.Equal(1, _commit.Commits);
        Assert.NotNull(result);
        Assert.NotNull(entitlement);
    }

    [Fact]
    public async Task An_override_with_an_unknown_feature_a_negative_limit_or_a_past_expiry_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Billing().GrantOverrideAsync(
            AccountId, new OperatorOverrideInput("TELEPORTS", false, 1, null, "x"), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() => Billing().GrantOverrideAsync(
            AccountId, new OperatorOverrideInput(BillingCapabilityCode.MemberSeat, false, -1, null, "x"), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() => Billing().GrantOverrideAsync(
            AccountId, new OperatorOverrideInput(BillingCapabilityCode.MemberSeat, false, 5, Now.AddMinutes(-1), "x"), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => Billing().GrantOverrideAsync(
            Guid.CreateVersion7(), new OperatorOverrideInput(BillingCapabilityCode.MemberSeat, false, 5, null, "x"), CancellationToken.None));
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task Revoking_a_manual_override_is_audited_and_a_second_revoke_is_a_no_op()
    {
        var entitlement = Entitlement.Create(AccountId, FeatureCode.Create(BillingCapabilityCode.MemberSeat), 50, EntitlementSource.Manual, Now);
        _entitlements.Add(entitlement);

        await Billing().RevokeOverrideAsync(entitlement.Id, "pilot over", entitlement.Version, CancellationToken.None);

        Assert.Equal(EntitlementStatus.Revoked, entitlement.Status);
        Assert.Equal("entitlement.revoke", Assert.Single(_audit.Records).Action);
        var commits = _commit.Commits;
        await Billing().RevokeOverrideAsync(entitlement.Id, "again", entitlement.Version, CancellationToken.None);
        Assert.Equal(commits, _commit.Commits);
    }

    [Fact]
    public async Task Revoke_refuses_a_stale_version_a_non_manual_entitlement_and_an_unknown_id()
    {
        var manual = Entitlement.Create(AccountId, FeatureCode.Create(BillingCapabilityCode.MemberSeat), 50, EntitlementSource.Manual, Now);
        var plan = Entitlement.Create(AccountId, FeatureCode.Create(BillingCapabilityCode.StorageGb), 5, EntitlementSource.Subscription, Now);
        _entitlements.AddRange([manual, plan]);

        await Assert.ThrowsAsync<ConflictException>(() => Billing().RevokeOverrideAsync(manual.Id, "x", manual.Version + 2, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => Billing().RevokeOverrideAsync(plan.Id, "x", plan.Version, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => Billing().RevokeOverrideAsync(Guid.CreateVersion7(), "x", 1, CancellationToken.None));
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task An_override_that_expired_no_longer_applies_and_the_plan_limit_returns()
    {
        var entitlement = Entitlement.Create(
            AccountId, FeatureCode.Create(BillingCapabilityCode.MemberSeat), 9999, EntitlementSource.Manual, Now.AddDays(-10),
            EntitlementTargetScope.Account, null, Now.AddDays(-1));
        _entitlements.Add(entitlement);
        _context.Setup(c => c.FeatureUsageLedger).Returns(TestBillingDbSet.Create(new List<Notrelix.Domain.Billing.Usage.FeatureUsageLedger>()).Object);
        var usage = new Mock<IBillingUsageReader>();
        usage.Setup(u => u.GetAccountUsageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingAccountUsage(3, 0, 0));
        var facts = new Notrelix.Application.Features.Billing.Entitlements.Services.BillingCapabilityFactsProvider(
            _context.Object, _clock.Object, usage.Object,
            new BillingPlanResolver(_context.Object, _clock.Object, new BillingPlanCatalogReader(_context.Object)));

        var fact = await facts.GetCapabilityAsync(AccountId, Guid.CreateVersion7(), BillingCapabilityCode.MemberSeat, 1, CancellationToken.None);

        Assert.Equal(10, fact!.Limit);
    }

    [Fact]
    public async Task An_active_override_wins_over_the_plan_limit_the_tenant_sees()
    {
        var entitlement = Entitlement.Create(AccountId, FeatureCode.Create(BillingCapabilityCode.MemberSeat), 9999, EntitlementSource.Manual, Now.AddDays(-1));
        _entitlements.Add(entitlement);
        _context.Setup(c => c.FeatureUsageLedger).Returns(TestBillingDbSet.Create(new List<Notrelix.Domain.Billing.Usage.FeatureUsageLedger>()).Object);
        var usage = new Mock<IBillingUsageReader>();
        usage.Setup(u => u.GetAccountUsageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingAccountUsage(3, 0, 0));
        var facts = new Notrelix.Application.Features.Billing.Entitlements.Services.BillingCapabilityFactsProvider(
            _context.Object, _clock.Object, usage.Object,
            new BillingPlanResolver(_context.Object, _clock.Object, new BillingPlanCatalogReader(_context.Object)));

        var fact = await facts.GetCapabilityAsync(AccountId, Guid.CreateVersion7(), BillingCapabilityCode.MemberSeat, 1, CancellationToken.None);

        Assert.Equal(9999, fact!.Limit);
    }
}
