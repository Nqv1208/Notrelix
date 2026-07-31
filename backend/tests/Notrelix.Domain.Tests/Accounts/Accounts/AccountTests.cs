using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Accounts;

[CoversAggregate(typeof(Account))]
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

    [Fact]
    public void InitialVersion_ShouldBe1()
    {
        var account = CreateAccount();

        account.Version.Should().Be(1);
    }

    // ── Rename ───────────────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Rename("New Name", _userId, _now);

        account.Name.Should().Be("New Name");
        account.DomainEvents.Should().ContainSingle(e => e is AccountRenamedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_SameName_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Rename("My Account", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Rename), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_ClosedAccount_ShouldThrow()
    {
        var account = CreateAccount();
        account.Archive(_userId, _now);

        var act = () => account.Rename("New Name", _userId, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename a closed account.");
    }

    [CoversMutation(typeof(Account), nameof(Account.Rename), MutationScenario.Version, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Rename_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.Rename("New Name", _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Rename("New Name", _userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountRenamedDomainEvent>()
            .Single();
        evt.OldName.Should().Be("My Account");
        evt.NewName.Should().Be("New Name");
    }

    // ── Archive ──────────────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldSucceed()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Archive(_userId, _now);

        account.Status.Should().Be(AccountStatus.Closed);
        account.DomainEvents.Should().ContainSingle(e => e is AccountArchivedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_AlreadyClosed_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.Archive(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Archive(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Archive_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.Archive(_userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Archive(_userId, _now);

        account.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_NoOp_VersionShouldNotIncrement()
    {
        var account = CreateAccount();
        account.Archive(_userId, _now);
        var versionBefore = account.Version;

        account.Archive(_userId, _now);

        account.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldSetAudit()
    {
        var account = CreateAccount();

        account.Archive(_userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
    }

    [CoversMutation(typeof(Account), nameof(Account.Archive), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Archive(_userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountArchivedDomainEvent>()
            .Single();
        evt.AccountId.Should().Be(account.Id);
    }

    // ── Suspend ──────────────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_ShouldSucceed()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.Status.Should().Be(AccountStatus.Suspended);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSuspendedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_AlreadySuspended_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.Suspend(_userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_ShouldIncrementVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Suspend(_userId, _now);

        account.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_NoOp_VersionShouldNotIncrement()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        var versionBefore = account.Version;

        account.Suspend(_userId, _now);

        account.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_ShouldSetAudit()
    {
        var account = CreateAccount();

        account.Suspend(_userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
    }

    [CoversMutation(typeof(Account), nameof(Account.Suspend), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Suspend(_userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountSuspendedDomainEvent>()
            .Single();
        evt.PreviousStatus.Should().Be(AccountStatus.Active);
    }

    // ── Activate ─────────────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.Activate), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_AlreadyActive_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Activate(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Activate_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.Activate(_userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.Activate), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldIncrementVersion()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        var versionBefore = account.Version;

        account.Activate(_userId, _now);

        account.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(Account), nameof(Account.Activate), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_NoOp_VersionShouldNotIncrement()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Activate(_userId, _now);

        account.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(Account), nameof(Account.Activate), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldSetAudit()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);

        account.Activate(_userId, _now);

        account.UpdatedAt.Should().Be(_now);
        account.UpdatedBy.Should().Be(_userId);
    }

    [CoversMutation(typeof(Account), nameof(Account.Activate), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        account.Suspend(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Activate(_userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountActivatedDomainEvent>()
            .Single();
        evt.PreviousStatus.Should().Be(AccountStatus.Suspended);
    }

    // ── Delete / Restore ──────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldMarkAsDeleted()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Delete(_userId, _now);

        account.IsDeleted.Should().BeTrue();
        account.DomainEvents.Should().ContainSingle(e => e is AccountDeletedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_DeletedAccount_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Delete(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldRestoreToActive()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Restore(_userId, _now);

        account.IsDeleted.Should().BeFalse();
        account.DomainEvents.Should().ContainSingle(e => e is AccountRestoredDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ActiveAccount_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Restore(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Delete(_userId, _now);

        account.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_NoOp_VersionShouldNotIncrement()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);
        var versionBefore = account.Version;

        account.Delete(_userId, _now);

        account.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetDeleteAudit()
    {
        var account = CreateAccount();

        account.Delete(_userId, _now);

        account.DeletedAt.Should().Be(_now);
        account.DeletedBy.Should().Be(_userId);
    }

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Delete(_userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountDeletedDomainEvent>()
            .Single();
        evt.Status.Should().Be(AccountStatus.Active);
        evt.DeletedBy.Should().Be(_userId);
    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);
        var versionBefore = account.Version;

        account.Restore(_userId, _now);

        account.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_NoOp_VersionShouldNotIncrement()
    {
        var account = CreateAccount();
        var versionBefore = account.Version;

        account.Restore(_userId, _now);

        account.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldSetRestoreAudit()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        account.Restore(_userId, _now);

    }

    [CoversMutation(typeof(Account), nameof(Account.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Restore(_userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountRestoredDomainEvent>()
            .Single();
        evt.RestoredBy.Should().Be(_userId);
    }

    // ── UpdatePlanCode ───────────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePlanCode_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode("enterprise", _userId, _now);

        account.PlanCode.Should().Be("enterprise");
        account.DomainEvents.Should().ContainSingle(e => e is AccountPlanCodeChangedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePlanCode_SameValue_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.UpdatePlanCode("enterprise", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode("enterprise", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePlanCode_ShouldTrimWhitespace()
    {
        var account = CreateAccount();

        account.UpdatePlanCode("  enterprise  ", _userId, _now);

        account.PlanCode.Should().Be("enterprise");
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Version, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void UpdatePlanCode_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.UpdatePlanCode("enterprise", _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdatePlanCode), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePlanCode_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        account.UpdatePlanCode("old-plan", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdatePlanCode("new-plan", _userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountPlanCodeChangedDomainEvent>()
            .Single();
        evt.OldPlanCode.Should().Be("old-plan");
        evt.NewPlanCode.Should().Be("new-plan");
    }

    // ── UpdateDefaultRegion ──────────────────────────────────────────────

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateDefaultRegion_ShouldSucceed_AndRaiseEvent()
    {
        var account = CreateAccount();
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.DefaultRegionCode.Should().Be("us-east-1");
        account.DomainEvents.Should().ContainSingle(e => e is AccountDefaultRegionChangedDomainEvent);
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateDefaultRegion_SameValue_ShouldNotRaiseEvent()
    {
        var account = CreateAccount();
        account.UpdateDefaultRegion("us-east-1", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateDefaultRegion_ShouldTrimWhitespace()
    {
        var account = CreateAccount();

        account.UpdateDefaultRegion("  us-east-1  ", _userId, _now);

        account.DefaultRegionCode.Should().Be("us-east-1");
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Version, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(Account), nameof(Account.Delete), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void UpdateDefaultRegion_AfterDelete_ShouldThrow()
    {
        var account = CreateAccount();
        account.Delete(_userId, _now);

        var act = () => account.UpdateDefaultRegion("us-east-1", _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Account), nameof(Account.UpdateDefaultRegion), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateDefaultRegion_ShouldRaiseEvent_WithCorrectPayload()
    {
        var account = CreateAccount();
        account.UpdateDefaultRegion("us-east-1", _userId, _now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.UpdateDefaultRegion("eu-west-1", _userId, _now);

        var evt = account.DomainEvents
            .OfType<AccountDefaultRegionChangedDomainEvent>()
            .Single();
        evt.OldRegionCode.Should().Be("us-east-1");
        evt.NewRegionCode.Should().Be("eu-west-1");
    }
}
