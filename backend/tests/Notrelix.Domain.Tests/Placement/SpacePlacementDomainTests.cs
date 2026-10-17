using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.Placement;

/// <summary>
/// Placement of Boards and Pages in Workspaces Spaces (docs/product/workspaces.md
/// §79): owned by the resource, versioned, evented, a no-op when unchanged,
/// rejected for archived resources; a nested page follows its root page.
/// </summary>
public class SpacePlacementDomainTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Board_PlaceInSpace_ChangesTheSpace_IncrementsTheVersion_AndRaisesTheEvent()
    {
        var board = Board.Create(AccountId, WorkspaceId, Actor, "Ledger", null, T0);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;
        var space = Guid.NewGuid();

        board.PlaceInSpace(space, Actor, T0.AddMinutes(1));

        board.SpaceId.Should().Be(space);
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BoardSpaceChangedDomainEvent>()
            .Which.NewSpaceId.Should().Be(space);
    }

    [Fact]
    public void Board_PlaceInTheCurrentSpace_IsANoOp()
    {
        var space = Guid.NewGuid();
        var board = Board.Create(AccountId, WorkspaceId, Actor, "Ledger", null, T0, spaceId: space);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;

        board.PlaceInSpace(space, Actor, T0.AddMinutes(1));

        board.Version.Should().Be(version);
        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Board_PlaceInSpace_WhenArchived_IsRejected_WithoutMutation()
    {
        var board = Board.Create(AccountId, WorkspaceId, Actor, "Ledger", null, T0);
        board.Archive(Actor, T0);

        var act = () => board.PlaceInSpace(Guid.NewGuid(), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Board_CannotChangeSpaceArchived);
        board.SpaceId.Should().BeNull();
    }

    [Fact]
    public void Page_PlaceInSpace_OfARootPage_ChangesTheSpace_AndRaisesTheEvent()
    {
        var page = Page.Create(AccountId, WorkspaceId, "Playbook", Actor, T0);
        ((IHasDomainEvents)page).ClearDomainEvents();
        var space = Guid.NewGuid();

        page.PlaceInSpace(space, Actor, T0.AddMinutes(1));

        page.SpaceId.Should().Be(space);
        page.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PageSpaceChangedDomainEvent>();
    }

    [Fact]
    public void Page_PlaceInSpace_OfANestedPage_IsRejected_ButFollowRootSpace_Applies()
    {
        var space = Guid.NewGuid();
        var nested = Page.Create(AccountId, WorkspaceId, "Chapter", Actor, T0, parentId: Guid.NewGuid());

        var act = () => nested.PlaceInSpace(space, Actor, T0.AddMinutes(1));
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_NestedPageFollowsRootSpace);
        nested.SpaceId.Should().BeNull();

        nested.FollowRootSpace(space, Actor, T0.AddMinutes(1));
        nested.SpaceId.Should().Be(space);
    }

    [Fact]
    public void Page_PlaceInSpace_WhenArchived_IsRejected()
    {
        var page = Page.Create(AccountId, WorkspaceId, "Playbook", Actor, T0);
        page.Archive(Actor, T0);

        var act = () => page.PlaceInSpace(Guid.NewGuid(), Actor, T0.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_CannotChangeSpaceArchived);
    }
}
