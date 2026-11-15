using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// Opening a page: a page that does not exist and a page deleted for good answer the same
/// <see cref="NotFoundException"/> on both reads of the route (the page and its blocks), so the client shows one
/// "no access" state. An unreadable page never reaches these handlers: the ViewPage requirement answers it with the
/// same not found (see the resource-level enforcement matrix).
/// </summary>
public sealed class OpenPageNotFoundTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task AMissingPage_AndAPurgedPage_GiveTheSameNotFound_OnBothReads()
    {
        var purged = Page.Create(AccountId, WorkspaceId, "Gone", UserId, Now);
        purged.Archive(UserId, Now.AddMinutes(1));
        purged.Delete(UserId, Now.AddMinutes(2));
        var (page, blocks) = Handlers(purged);

        foreach (var id in new[] { Guid.CreateVersion7(), purged.Id })
        {
            var pageRead = () => page.Handle(new GetPageQuery(id), CancellationToken.None);
            var blocksRead = () => blocks.Handle(new GetPageBlocksQuery(id), CancellationToken.None);

            var fromPage = (await pageRead.Should().ThrowAsync<NotFoundException>()).Which;
            var fromBlocks = (await blocksRead.Should().ThrowAsync<NotFoundException>()).Which;
            fromPage.GetType().Should().Be(fromBlocks.GetType());
        }
    }

    [Fact]
    public async Task APageWithoutBlocks_OpensAsAnEmptyList_NotAnError()
    {
        var empty = Page.Create(AccountId, WorkspaceId, "Empty", UserId, Now);
        var (_, blocks) = Handlers(empty);

        var window = (await blocks.Handle(new GetPageBlocksQuery(empty.Id), CancellationToken.None)).Data!;
        window.Items.Should().BeEmpty();
        window.NextCursor.Should().BeNull();
    }

    private static (GetPageQueryHandler Page, GetPageBlocksQueryHandler Blocks) Handlers(Page page)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(new List<Page> { page }).Object);
        context.Setup(c => c.Blocks).Returns(TestDbSet.Create(new List<Block>()).Object);
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleSpaceIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        return (new GetPageQueryHandler(context.Object, visibility.Object, requestContext, Mock.Of<Notrelix.Application.Features.Documents.Pages.Services.IPageEditAccess>()), new GetPageBlocksQueryHandler(context.Object));
    }
}
