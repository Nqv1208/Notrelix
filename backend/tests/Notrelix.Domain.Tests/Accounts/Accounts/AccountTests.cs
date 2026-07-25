using FluentAssertions;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private Account CreateAccount(
        string name = "My Account",
        string slug = "my-account",
        AccountType type = AccountType.Team)
    {
        return Account.Create(name, slug, type, _userId, _now);
    }

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSucceed()
    {
        var account = CreateAccount();

        account.Name.Should().Be("My Account");
        account.Slug.Should().Be("my-account");
        account.Type.Should().Be(AccountType.Team);
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithLegalName_ShouldSet()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Enterprise, _userId, _now, "My Corp Inc.");

        account.LegalName.Should().Be("My Corp Inc.");
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => Account.Create("", "my-account", AccountType.Team, _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_EmptySlug_ShouldThrow()
    {
        var act = () => Account.Create("My Account", "", AccountType.Team, _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldSetAuditOnCreate()
    {
        var account = CreateAccount();

        account.CreatedBy.Should().Be(_userId);
        account.CreatedAt.Should().Be(_now);
    }

    // ── Rename ───────────────────────────────────────────────────────────

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Rename("New Name", _userId, _now);

        account.Name.Should().Be("New Name");
        account.DomainEvents.Should().ContainSingle(e => e is AccountRenamedDomainEvent);
    }

    [Fact]
    public void Rename_SameName_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Rename("My Account", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_ClosedAccount_ShouldThrow()
    {
        var account = CreateAccount();
        account.Archive(_userId, _now);

        var act = () => account.Rename("New Name", _userId, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename a closed account.");
    }

    [Fact]
    public void Rename_ShouldSetAuditAndVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Rename("New Name", _userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
        account.Version.Should().Be(versionBefore + 1);
    }

    // ── Archive ──────────────────────────────────────────────────────────

    [Fact]
    public void Archive_ShouldSucceed()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Archive(_userId, _now);

        account.Status.Should().Be(AccountStatus.Closed);
        account.DomainEvents.Should().ContainSingle(e => e is AccountArchivedDomainEvent);
    }

    [Fact]
    public void Archive_AlreadyClosed_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.Archive(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Archive(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    // ── Suspend ──────────────────────────────────────────────────────────

    [Fact]
    public void Suspend_ShouldSucceed()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.Status.Should().Be(AccountStatus.Suspended);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSuspendedDomainEvent);
    }

    [Fact]
    public void Suspend_AlreadySuspended_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    // ── Activate ─────────────────────────────────────────────────────────

    [Fact]
    public void Activate_ShouldRestoreToActive_AndRaiseEvent()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Activate(_userId, _now);

        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountActivatedDomainEvent);
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Activate(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    // ── SoftDelete / Restore ─────────────────────────────────────────────

    [Fact]
    public void SoftDelete_ShouldMarkAsSoftDeleted()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.SoftDelete(_userId, _now);

        account.IsDeleted.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.SoftDeleted);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSoftDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_DeletedAccount_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.SoftDelete(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.SoftDelete(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestoreToActive()
    {
        var account = CreateAccount();
        account.SoftDelete(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Restore(_userId, _now);

        account.IsDeleted.Should().BeFalse();
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountRestoredDomainEvent);
    }

    [Fact]
    public void Restore_ActiveAccount_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Restore(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    // ── UpdatePlanCode ───────────────────────────────────────────────────

    [Fact]
    public void UpdatePlanCode_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode("enterprise", _userId, _now);

        account.PlanCode.Should().Be("enterprise");
        account.DomainEvents.Should().ContainSingle(e => e is AccountPlanCodeChangedDomainEvent);
    }

    [Fact]
    public void UpdatePlanCode_SameValue_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.UpdatePlanCode("enterprise", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode("enterprise", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePlanCode_ShouldTrimWhitespace()
    {
        var account = CreateAccount();

        account.UpdatePlanCode("  enterprise  ", _userId, _now);

        account.PlanCode.Should().Be("enterprise");
    }

    [Fact]
    public void UpdatePlanCode_Null_ShouldClear()
    {
        var account = CreateAccount();
        account.UpdatePlanCode("enterprise", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode(null, _userId, _now);

        account.PlanCode.Should().BeNull();
        account.DomainEvents.Should().ContainSingle(e => e is AccountPlanCodeChangedDomainEvent);
    }

    [Fact]
    public void UpdatePlanCode_ShouldSetAuditAndVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.UpdatePlanCode("enterprise", _userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
        account.Version.Should().Be(versionBefore + 1);
    }

    // ── UpdateDefaultRegion ──────────────────────────────────────────────

    [Fact]
    public void UpdateDefaultRegion_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.DefaultRegionCode.Should().Be("us-east-1");
        account.DomainEvents.Should().ContainSingle(e => e is AccountDefaultRegionChangedDomainEvent);
    }

    [Fact]
    public void UpdateDefaultRegion_SameValue_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.UpdateDefaultRegion("us-east-1", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDefaultRegion_ShouldTrimWhitespace()
    {
        var account = CreateAccount();

        account.UpdateDefaultRegion("  us-east-1  ", _userId, _now);

        account.DefaultRegionCode.Should().Be("us-east-1");
    }

    [Fact]
    public void UpdateDefaultRegion_Null_ShouldClear()
    {
        var account = CreateAccount();
        account.UpdateDefaultRegion("us-east-1", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion(null, _userId, _now);

        account.DefaultRegionCode.Should().BeNull();
        account.DomainEvents.Should().ContainSingle(e => e is AccountDefaultRegionChangedDomainEvent);
    }

    [Fact]
    public void UpdateDefaultRegion_ShouldSetAuditAndVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
        account.Version.Should().Be(versionBefore + 1);
    }
}
