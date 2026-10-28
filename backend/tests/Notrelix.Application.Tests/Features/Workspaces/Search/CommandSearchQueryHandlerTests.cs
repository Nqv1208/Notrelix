using Notrelix.Application.Features.Documents.Public.PageSearch;
using Notrelix.Application.Features.WorkManagement.Public.CommandSearch;
using Notrelix.Application.Features.Workspaces.Search.Queries.CommandSearch;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Workspaces.Search;

public class CommandSearchQueryHandlerTests
{
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _me = Guid.CreateVersion7();
    private readonly FakeResourceVisibility _visibility = new();
    private readonly Mock<IWorkSearchFacts> _work = new();
    private readonly Mock<IPageSearchFacts> _pages = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly CommandSearchQueryHandler _handler;

    public CommandSearchQueryHandlerTests()
    {
        _context.Setup(c => c.UserId).Returns(_me);
        _work.Setup(w => w.SearchAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkSearchResult.Empty);
        _pages.Setup(p => p.SearchAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) => ids.Select(id => new ActorSnapshot(id, "Person " + id.ToString("N")[..4], null)).ToList());
        _handler = new CommandSearchQueryHandler(_visibility, _work.Object, _pages.Object, _actors.Object, _context.Object);
    }

    private Task<Notrelix.Application.Common.Models.Result<CommandSearchDto>> Run(string? text) =>
        _handler.Handle(new CommandSearchQuery(_workspace, text), CancellationToken.None);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" a ")]
    [InlineData("   ")]
    public async Task ShortOrMissingText_AnswersEmpty_WithoutAskingAnyContext(string? text)
    {
        _visibility.Boards.Add(Guid.CreateVersion7());

        var result = await Run(text);

        result.Succeeded.Should().BeTrue();
        result.Data!.Work.Should().BeEmpty();
        result.Data.Docs.Should().BeEmpty();
        result.Data.Boards.Should().BeEmpty();
        _work.VerifyNoOtherCalls();
        _pages.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TheContextsAreAskedOnlyWithTheIdsTheCallerMaySee()
    {
        var board = Guid.CreateVersion7();
        var page = Guid.CreateVersion7();
        _visibility.Boards.Add(board);
        _visibility.Pages.Add(page);

        await Run("  hoa   don ");

        _work.Verify(w => w.SearchAsync(_workspace, _me, It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == board), "hoa don",
            CommandSearchQueryHandler.WorkLimit, CommandSearchQueryHandler.BoardLimit, It.IsAny<CancellationToken>()), Times.Once);
        _pages.Verify(p => p.SearchAsync(_workspace, It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == page), "hoa don",
            CommandSearchQueryHandler.DocLimit, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithNothingReadable_NoContextIsAsked()
    {
        var result = await Run("budget");

        result.Data!.Work.Should().BeEmpty();
        _work.VerifyNoOtherCalls();
        _pages.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResultsAreMapped_WithNamesAndOnlyDisclosedSpaces()
    {
        var board = Guid.CreateVersion7();
        var page = Guid.CreateVersion7();
        var visibleSpace = Guid.CreateVersion7();
        var hiddenSpace = Guid.CreateVersion7();
        var colleague = Guid.CreateVersion7();
        _visibility.Boards.Add(board);
        _visibility.Pages.Add(page);
        _visibility.Spaces.Add(visibleSpace);
        var itemId = Guid.CreateVersion7();
        _work.Setup(w => w.SearchAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkSearchResult(
                [
                    new WorkSearchItem(itemId, "ERP-1", "Hóa đơn", board, "Kế toán", colleague, false, "Doing", "in_progress", null, null),
                    new WorkSearchItem(Guid.CreateVersion7(), null, "Parent", board, "Kế toán", null, true, null, null, null, "Invoice template"),
                ],
                [new WorkSearchBoard(board, "Kế toán", hiddenSpace, 2), new WorkSearchBoard(Guid.CreateVersion7(), "Other", visibleSpace, 0)]));
        _pages.Setup(p => p.SearchAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PageSearchHit(page, "Quy trình", hiddenSpace, ["Handbook", "Finance"], DateTimeOffset.UtcNow, colleague)]);

        var data = (await Run("hoa don")).Data!;

        data.Work[0].Should().Match<CommandSearchWorkDto>(w => w.Id == itemId && w.AssigneeName != null && w.Match.Kind == "title" && w.StatusCategory == "in_progress");
        data.Work[1].Should().Match<CommandSearchWorkDto>(w => w.Mine && w.AssigneeName == null && w.Match.Kind == "child" && w.Match.Snippet == "Invoice template");
        data.Docs.Should().ContainSingle().Which.Should().Match<CommandSearchDocDto>(
            d => d.SpaceId == null && d.Location == "Handbook / Finance" && d.EditedByName != null && d.Match.Kind == "title");
        data.Boards[0].SpaceId.Should().BeNull("the Space is not visible to the caller");
        data.Boards[1].SpaceId.Should().Be(visibleSpace);
        data.Boards[0].MatchedItems.Should().Be(2);
    }

    [Fact]
    public void ReadsTheWorkspaceWithViewWorkspace_ForTheCallersWorkspaceOnly()
    {
        var query = new CommandSearchQuery(_workspace, "x");
        var requirement = (IRequirePermission)query;

        requirement.Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ViewWorkspace);
        requirement.Resource.ResourceId.Should().Be(_workspace);
        requirement.Resource.Kind.Value.Should().Be("workspaces.workspace");
    }
}
