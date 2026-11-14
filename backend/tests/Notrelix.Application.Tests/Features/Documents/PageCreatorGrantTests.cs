using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Testing.Application.Fakes;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>The creator of a page is granted Manager on it in the same request (the integration test checks the stored entry).</summary>
public sealed class PageCreatorGrantTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task CreatingAPage_GrantsTheCreatorManagerOfThatPage()
    {
        var pages = new List<Notrelix.Domain.Documents.Pages.Page>();
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create(pages).Object);
        var grant = new Mock<IResourceCreatorGrant>();
        var requestContext = new FakeCurrentRequestContext().AsUser(UserId);
        requestContext.Tenant.SetWorkspace(AccountId, WorkspaceId, UserId);
        var sut = new CreatePageCommandHandler(context.Object, new Mock<IPageSpacePlacement>().Object,
            new InMemoryPageSubtreeStore(), grant.Object, requestContext, new FixedClock());

        var result = await sut.Handle(new CreatePageCommand(WorkspaceId, "Mine", null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        grant.Verify(g => g.GrantManager(AccountId, WorkspaceId, "documents.page", result.Data, UserId, Now.AddMinutes(5)), Times.Once);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
