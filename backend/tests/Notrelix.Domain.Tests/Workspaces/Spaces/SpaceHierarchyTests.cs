using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces.Spaces;

/// <summary>
/// WSP-021 (hierarchy, ordering, sibling names), WSP-022 (archive/delete never
/// cascade) and WSP-024 (membership, last-Admin) on the Space aggregate, with
/// every external fact supplied explicitly as Application would.
/// </summary>
public class SpaceHierarchyTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

    private static Space NewSpace(string name, SpacePlacement? placement = null, Guid? createdBy = null) =>
        Space.Create(AccountId, WorkspaceId, name, SpaceVisibility.Private, createdBy ?? Actor, T0, SpaceType.Department, placement: placement);

    private static SpaceParentFact ParentFact(Space parent, params Guid[] ancestorsAboveParent) =>
        new(parent.Id, parent.WorkspaceId, parent.Status == SpaceStatus.Active, [.. ancestorsAboveParent, parent.Id]);

    private static FractionalIndex Key(string value) => FractionalIndex.Create(value);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_UnderAParent_RecordsParentAndPosition_AndTheCreatorIsTheFirstAdmin()
    {
        var it = NewSpace("IT");

        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a1"), ["Network"]));

        security.ParentSpaceId.Should().Be(it.Id);
        security.Position.Value.Should().Be("a1");
        security.Members.Should().ContainSingle().Which.Should().Match<SpaceMember>(m =>
            m.SubjectType == SpaceMemberSubjectType.User && m.SubjectId == Actor && m.Role == SpaceRole.Admin);
        security.DomainEvents.OfType<SpaceMemberAddedDomainEvent>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("security")]
    [InlineData("  SECURITY ")]
    public void Create_WithASiblingNameAlreadyUsed_CaseInsensitively_IsRejected(string name)
    {
        var it = NewSpace("IT");

        var act = () => NewSpace(name, new SpacePlacement(ParentFact(it), Key("a1"), ["Security"]));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName);
    }

    [Fact]
    public void Create_UnderAParentOfAnotherWorkspace_IsRejected()
    {
        var foreignParent = new SpaceParentFact(Guid.NewGuid(), Guid.NewGuid(), true, []);

        var act = () => NewSpace("Child", new SpacePlacement(foreignParent, Key("a0"), []));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_ParentInOtherWorkspace);
    }

    [Fact]
    public void Create_UnderAnArchivedParent_IsRejected()
    {
        var it = NewSpace("IT");
        it.Archive(Actor, T0, SpaceContentFact.Empty);

        var act = () => NewSpace("Child", new SpacePlacement(ParentFact(it), Key("a0"), []));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_ParentNotActive);
    }

    // ── Move ──────────────────────────────────────────────────────────────

    [Fact]
    public void Move_UnderItsOwnDescendant_IsRejected_WithoutMutation()
    {
        var it = NewSpace("IT");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));
        ((IHasDomainEvents)it).ClearDomainEvents();
        var version = it.Version;

        // Moving IT under Security: Security's ancestor path contains IT.
        var act = () => it.Move(new SpacePlacement(ParentFact(security, it.Id), Key("a0"), []), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MoveCreatesCycle);
        it.ParentSpaceId.Should().BeNull();
        it.Version.Should().Be(version);
        it.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Move_UnderItself_IsRejected()
    {
        var it = NewSpace("IT");

        var act = () => it.Move(new SpacePlacement(ParentFact(it), Key("a0"), []), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MoveCreatesCycle);
    }

    [Fact]
    public void Move_ToTheCurrentParentAndPosition_IsASemanticNoOp()
    {
        var it = NewSpace("IT");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));
        ((IHasDomainEvents)security).ClearDomainEvents();
        var version = security.Version;

        security.Move(new SpacePlacement(ParentFact(it), Key("a0"), []), Actor, T0.AddMinutes(1));

        security.Version.Should().Be(version);
        security.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Move_ToANewParent_RepointsTheParent_IncrementsTheVersion_AndRaisesSpaceMoved()
    {
        var it = NewSpace("IT");
        var finance = NewSpace("Finance");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));
        ((IHasDomainEvents)security).ClearDomainEvents();
        var version = security.Version;

        security.Move(new SpacePlacement(ParentFact(finance), Key("a5"), ["Audit"]), Actor, T0.AddMinutes(1));

        security.ParentSpaceId.Should().Be(finance.Id);
        security.Position.Value.Should().Be("a5");
        security.Version.Should().Be(version + 1);
        security.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SpaceMovedDomainEvent>()
            .Which.Should().Match<SpaceMovedDomainEvent>(e => e.OldParentSpaceId == it.Id && e.NewParentSpaceId == finance.Id);
    }

    [Fact]
    public void Move_ToTheRoot_ClearsTheParent()
    {
        var it = NewSpace("IT");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));

        security.Move(SpacePlacement.Root(Key("a3"), ["IT"]), Actor, T0.AddMinutes(1));

        security.ParentSpaceId.Should().BeNull();
    }

    [Fact]
    public void Move_ToAParentWhereTheNameIsTaken_IsRejected()
    {
        var it = NewSpace("IT");
        var finance = NewSpace("Finance");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));

        var act = () => security.Move(new SpacePlacement(ParentFact(finance), Key("a1"), ["security"]), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName);
        security.ParentSpaceId.Should().Be(it.Id);
    }

    [Fact]
    public void Move_OfAnArchivedSpace_IsRejected()
    {
        var it = NewSpace("IT");
        it.Archive(Actor, T0, SpaceContentFact.Empty);

        var act = () => it.Move(SpacePlacement.Root(Key("a1"), []), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotMoveArchived);
    }

    [Fact]
    public void Rename_ToANameUsedByASibling_IsRejected_WithoutMutation()
    {
        var it = NewSpace("IT");
        var version = it.Version;

        var act = () => it.Rename("finance", Actor, T0.AddMinutes(1), ["Finance"]);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName);
        it.Name.Should().Be("IT");
        it.Version.Should().Be(version);
    }

    // ── Archive / delete / reactivate (WSP-022) ───────────────────────────

    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(0, 0, 1, 1)]
    public void Archive_WithActiveChildrenOrResources_IsRejected_WithoutMutation(int activeChildren, int existingChildren, int activeResources, int existingResources)
    {
        var it = NewSpace("IT");
        var version = it.Version;

        var act = () => it.Archive(Actor, T0.AddMinutes(1), new SpaceContentFact(activeChildren, existingChildren, activeResources, existingResources));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotArchiveWithActiveContent);
        it.Status.Should().Be(SpaceStatus.Active);
        it.Version.Should().Be(version);
    }

    [Fact]
    public void Archive_WithOnlyArchivedChildrenAndResources_Succeeds()
    {
        var it = NewSpace("IT");

        it.Archive(Actor, T0.AddMinutes(1), new SpaceContentFact(0, 2, 0, 3));

        it.Status.Should().Be(SpaceStatus.Archived);
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(0, 0, 0, 1)]
    public void Delete_WhileAnythingIsStillPlacedInIt_IsRejected_WithoutMutation(int activeChildren, int existingChildren, int activeResources, int existingResources)
    {
        var it = NewSpace("IT");

        var act = () => it.Delete(Actor, T0.AddMinutes(1), new SpaceContentFact(activeChildren, existingChildren, activeResources, existingResources));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotDeleteWithContent);
        it.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Unarchive_UnderAnInactiveParent_IsRejected()
    {
        var it = NewSpace("IT");
        it.Archive(Actor, T0, SpaceContentFact.Empty);

        var act = () => it.Unarchive(Actor, T0.AddMinutes(1), parentIsActive: false);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_ParentMustBeActiveToReactivate);
        it.Status.Should().Be(SpaceStatus.Archived);
    }

    [Fact]
    public void Restore_UnderAnInactiveParent_OrWithATakenName_IsRejected_AndOtherwiseTakesTheGivenPosition()
    {
        var it = NewSpace("IT");
        var security = NewSpace("Security", new SpacePlacement(ParentFact(it), Key("a0"), []));
        security.Delete(Actor, T0.AddMinutes(1), SpaceContentFact.Empty);

        var inactiveParent = new SpaceParentFact(it.Id, WorkspaceId, IsActive: false, [it.Id]);
        var underInactive = () => security.Restore(Actor, T0.AddMinutes(2), new SpacePlacement(inactiveParent, Key("a0"), []));
        underInactive.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_ParentNotActive);

        var nameTaken = () => security.Restore(Actor, T0.AddMinutes(2), new SpacePlacement(ParentFact(it), Key("a0"), ["Security"]));
        nameTaken.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName);
        security.IsDeleted.Should().BeTrue();

        security.Restore(Actor, T0.AddMinutes(2), new SpacePlacement(ParentFact(it), Key("a2"), ["Network"]));

        security.IsDeleted.Should().BeFalse();
        security.Position.Value.Should().Be("a2");
    }

    // ── Membership (WSP-024) ──────────────────────────────────────────────

    [Fact]
    public void AddMember_AddsAUserOrTeamEntry_IncrementsTheVersion_AndRaisesTheEvent()
    {
        var it = NewSpace("IT");
        ((IHasDomainEvents)it).ClearDomainEvents();
        var version = it.Version;
        var team = Guid.NewGuid();

        it.AddMember(SpaceMemberSubjectType.Team, team, SpaceRole.Member, Actor, T0.AddMinutes(1));

        it.Members.Should().Contain(m => m.SubjectType == SpaceMemberSubjectType.Team && m.SubjectId == team && m.Role == SpaceRole.Member);
        it.Version.Should().Be(version + 1);
        it.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SpaceMemberAddedDomainEvent>();
    }

    [Fact]
    public void AddMember_Again_WithTheSameRole_IsANoOp_ButWithAnotherRole_IsRejected()
    {
        var it = NewSpace("IT");
        var user = Guid.NewGuid();
        it.AddMember(SpaceMemberSubjectType.User, user, SpaceRole.Viewer, Actor, T0.AddMinutes(1));
        ((IHasDomainEvents)it).ClearDomainEvents();
        var version = it.Version;

        it.AddMember(SpaceMemberSubjectType.User, user, SpaceRole.Viewer, Actor, T0.AddMinutes(2));
        it.Version.Should().Be(version);
        it.DomainEvents.Should().BeEmpty();

        var act = () => it.AddMember(SpaceMemberSubjectType.User, user, SpaceRole.Admin, Actor, T0.AddMinutes(3));
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MemberAlreadyExists);
    }

    [Fact]
    public void RemovingOrDowngradingTheLastAdmin_IsRejected_WithoutMutation()
    {
        var it = NewSpace("IT");
        var version = it.Version;

        var remove = () => it.RemoveMember(SpaceMemberSubjectType.User, Actor, Actor, T0.AddMinutes(1));
        remove.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotRemoveLastAdmin);

        var downgrade = () => it.ChangeMemberRole(SpaceMemberSubjectType.User, Actor, SpaceRole.Member, Actor, T0.AddMinutes(1));
        downgrade.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotDowngradeLastAdmin);

        it.Members.Should().ContainSingle(m => m.Role == SpaceRole.Admin);
        it.Version.Should().Be(version);
    }

    [Fact]
    public void ATeamAdminEntry_CountsAsAnAdmin_SoTheUserAdminCanStepDown()
    {
        var it = NewSpace("IT");
        var team = Guid.NewGuid();
        it.AddMember(SpaceMemberSubjectType.Team, team, SpaceRole.Admin, Actor, T0.AddMinutes(1));

        it.ChangeMemberRole(SpaceMemberSubjectType.User, Actor, SpaceRole.Viewer, Actor, T0.AddMinutes(2));
        it.RemoveMember(SpaceMemberSubjectType.User, Actor, Actor, T0.AddMinutes(3));

        it.Members.Should().ContainSingle().Which.SubjectId.Should().Be(team);
    }

    [Fact]
    public void RemoveMember_OfAnAbsentSubject_IsANoOp_AndChangeRole_OfAnAbsentSubject_IsRejected()
    {
        var it = NewSpace("IT");
        var version = it.Version;

        it.RemoveMember(SpaceMemberSubjectType.User, Guid.NewGuid(), Actor, T0.AddMinutes(1));
        it.Version.Should().Be(version);

        var act = () => it.ChangeMemberRole(SpaceMemberSubjectType.Team, Guid.NewGuid(), SpaceRole.Viewer, Actor, T0.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MemberNotFound);
    }

    [Fact]
    public void MembershipChanges_OnAnArchivedSpace_AreRejected()
    {
        var it = NewSpace("IT");
        it.Archive(Actor, T0, SpaceContentFact.Empty);

        var act = () => it.AddMember(SpaceMemberSubjectType.User, Guid.NewGuid(), SpaceRole.Member, Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_CannotChangeMembersArchived);
    }
}
