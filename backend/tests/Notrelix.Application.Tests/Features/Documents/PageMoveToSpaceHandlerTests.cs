using Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Move a root page and its subtree to a Space: the no-op, the refusals that change nothing (a page below the
/// caller may not edit, a Space of another workspace, a nested page) and the deleted page that is skipped.
/// </summary>
public sealed class PageMoveToSpaceHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Move_CarriesEveryActivePageBelow_AndSkipsADeletedOne()
    {
        var (root, child, deleted) = Tree();
        deleted.Archive(Guid.CreateVersion7(), Now);
        deleted.Delete(Guid.CreateVersion7(), Now);
        var space = Guid.CreateVersion7();
        var subtree = new RecordingSubtreeAuthorizer();

        var result = await Handler(root, subtree, child, deleted).Handle(new MovePageToSpaceCommand(root.Id, space, root.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        root.SpaceId.Should().Be(space);
        child.SpaceId.Should().Be(space);
        deleted.SpaceId.Should().BeNull("a deleted page is not moved");
        subtree.Checked.Should().Equal(child.Id);
    }

    [Fact]
    public async Task Move_ToTheSameSpace_IsANoOp_WithoutVersionBumpOrChecks()
    {
        var (root, child, _) = Tree();
        var subtree = new RecordingSubtreeAuthorizer();
        var rootVersion = root.Version;
        var childVersion = child.Version;

        var result = await Handler(root, subtree, child).Handle(new MovePageToSpaceCommand(root.Id, root.SpaceId, rootVersion), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        root.Version.Should().Be(rootVersion);
        child.Version.Should().Be(childVersion);
        subtree.Checked.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_IsRefused_WhenThePersonMayNotEditAPageBelow_AndNothingMoves()
    {
        var (root, child, _) = Tree();
        var space = Guid.CreateVersion7();
        var refusing = new Mock<IPageSubtreeAuthorizer>();
        refusing.Setup(s => s.EnsureAllowedAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Func<Guid, MovePageToSpaceCommand>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Not permitted.", PageSubtreeAuthorizer.SubtreeNotPermittedCode));

        var act = () => Handler(root, refusing.Object, child).Handle(new MovePageToSpaceCommand(root.Id, space, root.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be(PageSubtreeAuthorizer.SubtreeNotPermittedCode);
        root.SpaceId.Should().BeNull();
        child.SpaceId.Should().BeNull();
    }

    [Fact]
    public async Task Move_ToASpaceOfAnotherWorkspace_IsRefused_AndLeavesThePageUnchanged()
    {
        var (root, child, _) = Tree();
        var foreignSpace = Guid.CreateVersion7();
        var placement = new Mock<IPageSpacePlacement>();
        placement.Setup(p => p.AuthorizeTargetAsync(root.WorkspaceId, foreignSpace, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Space", foreignSpace));
        var version = root.Version;

        var sut = new MovePageToSpaceCommandHandler(new InMemoryPageSubtreeStore(root, child), placement.Object, new RecordingSubtreeAuthorizer(), PageRequestContext.For(root), new FixedClock());
        var act = () => sut.Handle(new MovePageToSpaceCommand(root.Id, foreignSpace, version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("the same answer as for a Space that does not exist");
        root.SpaceId.Should().BeNull();
        root.Version.Should().Be(version);
    }

    [Fact]
    public async Task Move_OfAPageOfAnotherWorkspace_IsNotFound()
    {
        var (root, _, _) = Tree();
        var other = Page.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Elsewhere", Guid.CreateVersion7(), Now);

        var act = () => Handler(root, new RecordingSubtreeAuthorizer(), other).Handle(new MovePageToSpaceCommand(other.Id, Guid.CreateVersion7(), other.Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Move_OfANestedPage_IsRefused_BecauseItFollowsItsRoot()
    {
        var (root, child, _) = Tree();

        var act = () => Handler(root, new RecordingSubtreeAuthorizer(), child).Handle(new MovePageToSpaceCommand(child.Id, Guid.CreateVersion7(), child.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_Page_NestedPageFollowsRootSpace);
    }

    private static (Page Root, Page Child, Page Grandchild) Tree()
    {
        var account = Guid.CreateVersion7();
        var workspace = Guid.CreateVersion7();
        var author = Guid.CreateVersion7();
        var root = Page.Create(account, workspace, "Root", author, Now);
        var child = Page.Create(account, workspace, "Child", author, Now, parentId: root.Id);
        var third = Page.Create(account, workspace, "Third", author, Now, parentId: root.Id);
        return (root, child, third);
    }

    private static MovePageToSpaceCommandHandler Handler(Page context, IPageSubtreeAuthorizer subtree, params Page[] pages) =>
        new(new InMemoryPageSubtreeStore([context, .. pages.Where(p => p.Id != context.Id)]), new Mock<IPageSpacePlacement>().Object, subtree, PageRequestContext.For(context), new FixedClock());

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
