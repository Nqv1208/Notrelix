using FluentAssertions;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing;

public class PlanRevisionTests
{
    private readonly DateTimeOffset _now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private Plan New(string family = "pro", int revision = 1) =>
        Plan.Create(family, revision, "Pro", SubscriptionTier.Pro, Money.Create(100, "VND"), BillingPeriod.Monthly, _now);

    [Fact]
    public void Create_records_family_revision_and_tier()
    {
        var plan = New(" Pro-Team ", 3);

        plan.FamilyKey.Should().Be("pro-team");
        plan.Revision.Should().Be(3);
        plan.Tier.Should().Be(SubscriptionTier.Pro);
        plan.Status.Should().Be(PlanStatus.Active);
    }

    [Theory]
    [InlineData("1pro")]
    [InlineData("Pro Plan")]
    [InlineData("pro_plan")]
    public void Create_rejects_a_family_key_that_is_not_lowercase_letters_digits_and_dashes(string key)
    {
        var act = () => New(key);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Plan_FamilyKeyInvalid);
    }

    [Fact]
    public void Create_rejects_a_revision_below_one_and_a_negative_price()
    {
        var revision = () => New("pro", 0);
        var price = () => Plan.Create("pro", 1, "Pro", SubscriptionTier.Pro, Money.Create(-1, "VND"), BillingPeriod.Monthly, _now);

        revision.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Plan_RevisionInvalid);
        price.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Plan_PriceCannotBeNegative);
    }

    [Fact]
    public void An_unlimited_limit_carries_no_number()
    {
        var plan = New();

        plan.AddLimit(FeatureCode.Create("MEMBER_SEAT"), 500, _now, isUnlimited: true);

        var limit = plan.Limits.Single();
        limit.IsUnlimited.Should().BeTrue();
        limit.Limit.Should().Be(0);
    }

    [Fact]
    public void Two_limits_for_one_feature_are_rejected_and_leave_the_plan_unchanged()
    {
        var plan = New();
        plan.AddLimit(FeatureCode.Create("MEMBER_SEAT"), 5, _now);
        var version = plan.Version;

        var act = () => plan.AddLimit(FeatureCode.Create("member_seat"), 6, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Plan_FeatureAlreadyAdded);
        plan.Limits.Should().HaveCount(1);
        plan.Version.Should().Be(version);
    }

    [Fact]
    public void Retiring_a_plan_twice_is_a_no_op_and_keeps_the_limits()
    {
        var plan = New();
        plan.AddLimit(FeatureCode.Create("MEMBER_SEAT"), 5, _now);

        plan.Archive(_now);
        var version = plan.Version;
        plan.Archive(_now);

        plan.Status.Should().Be(PlanStatus.Archived);
        plan.Version.Should().Be(version);
        plan.Limits.Should().HaveCount(1);
    }
}
