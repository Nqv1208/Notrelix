using FluentAssertions;
using Notrelix.Domain.Operators;

namespace Notrelix.Domain.Tests.Operators;

public class OperatorAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private static OperatorAccount New() => OperatorAccount.Create(" Ops@Notrelix.TEST ", " Ops ", "hash", Now);

    [Fact]
    public void Create_normalizes_the_email_and_starts_active_and_unlocked()
    {
        var account = New();

        account.Email.Should().Be("ops@notrelix.test");
        account.DisplayName.Should().Be("Ops");
        account.Status.Should().Be(OperatorStatus.Active);
        account.CanAttemptSignIn(Now).Should().BeTrue();
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("@x.com")]
    [InlineData("x@")]
    public void Create_rejects_an_invalid_email(string email)
    {
        var act = () => OperatorAccount.Create(email, "Ops", "hash", Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(OperatorRuleCodes.Operators_Operator_EmailInvalid);
    }

    [Fact]
    public void Five_failed_sign_ins_lock_the_account_for_fifteen_minutes_then_it_opens_again()
    {
        var account = New();
        for (var i = 0; i < OperatorAccount.MaxFailedSignIns; i++) account.RecordFailedSignIn(Now);

        account.IsLockedAt(Now).Should().BeTrue();
        account.CanAttemptSignIn(Now.AddMinutes(14)).Should().BeFalse();
        account.CanAttemptSignIn(Now.Add(OperatorAccount.LockDuration).AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void A_failure_after_an_expired_lock_starts_a_fresh_count()
    {
        var account = New();
        for (var i = 0; i < OperatorAccount.MaxFailedSignIns; i++) account.RecordFailedSignIn(Now);

        account.RecordFailedSignIn(Now.AddHours(1));

        account.FailedSignInCount.Should().Be(1);
        account.IsLockedAt(Now.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void A_good_sign_in_clears_failures_and_the_lock_and_bumps_the_version()
    {
        var account = New();
        account.RecordFailedSignIn(Now);
        var version = account.Version;

        account.RecordSignedIn(Now.AddMinutes(1));

        account.FailedSignInCount.Should().Be(0);
        account.LockedUntil.Should().BeNull();
        account.LastSignedInAt.Should().Be(Now.AddMinutes(1));
        account.Version.Should().Be(version + 1);
    }
}

public class OperatorAuditEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_keeps_actor_target_reason_and_before_after()
    {
        var op = Guid.CreateVersion7();
        var target = Guid.CreateVersion7();

        var entry = OperatorAuditEntry.Record(op, "account.suspend", "account", target, target, "  fraud review ", "{\"status\":\"Active\"}", "{\"status\":\"Suspended\"}", Now);

        entry.OperatorId.Should().Be(op);
        entry.Reason.Should().Be("fraud review");
        entry.BeforeJson.Should().Contain("Active");
        entry.AfterJson.Should().Contain("Suspended");
        entry.OccurredAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_requires_a_reason(string? reason)
    {
        var act = () => OperatorAuditEntry.Record(Guid.CreateVersion7(), "a", "account", Guid.CreateVersion7(), null, reason!, null, null, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(OperatorRuleCodes.Operators_Audit_ReasonRequired);
    }

    [Fact]
    public void Record_limits_the_reason_to_500_characters()
    {
        var act = () => OperatorAuditEntry.Record(Guid.CreateVersion7(), "a", "account", Guid.CreateVersion7(), null, new string('x', 501), null, null, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(OperatorRuleCodes.Operators_Audit_ReasonTooLong);
    }

    [Fact]
    public void An_audit_entry_has_no_public_mutator()
    {
        typeof(OperatorAuditEntry).GetProperties().Should().OnlyContain(p => p.SetMethod == null || !p.SetMethod.IsPublic);
        typeof(OperatorAuditEntry).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName).Should().BeEmpty("the entry is append-only: nothing can change it after it is recorded");
    }
}
