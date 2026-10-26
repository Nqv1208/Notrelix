using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>The thread list says which threads are resolved, so a client can fold them away.</summary>
public class GetCommentsResolvedTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _page = Guid.CreateVersion7();
    private readonly List<Comment> _rows = [];
    private readonly Mock<ICollaborationDbContext> _context = new();
    private readonly Mock<IActorLookupService> _actors = new();

    public GetCommentsResolvedTests()
    {
        _context.Setup(c => c.Comments).Returns(TestDbSet.Create(_rows).Object);
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_me, "Me", null)]);
    }

    private Comment OnPage(string text)
    {
        var target = ResourceRef.Create(ResourceKind.Create("documents.page"), _page, _workspace);
        var comment = Comment.Create(_account, _workspace, target, text, _me, Now);
        _rows.Add(comment);
        return comment;
    }

    [Fact]
    public async Task Handle_ReportsAResolvedThreadWithTheTimeItWasResolved_AndAnOpenOneWithout()
    {
        var open = OnPage("still open");
        var resolved = OnPage("settled");
        resolved.Resolve(_me, Now.AddHours(2));

        var result = await new GetCommentsQueryHandler(_context.Object, _actors.Object)
            .Handle(GetCommentsQuery.ForPage(_page), CancellationToken.None);

        var byText = result.Data!.ToDictionary(c => c.ContentMd);
        byText["still open"].ResolvedAt.Should().BeNull();
        byText["settled"].ResolvedAt.Should().Be(Now.AddHours(2).DateTime);
        open.Id.Should().NotBe(resolved.Id);
    }

    [Fact]
    public async Task Handle_ReportsAReopenedThreadAsOpenAgain()
    {
        var comment = OnPage("back again");
        comment.Resolve(_me, Now.AddHours(1));
        comment.Reopen(_me, Now.AddHours(2));

        var result = await new GetCommentsQueryHandler(_context.Object, _actors.Object)
            .Handle(GetCommentsQuery.ForPage(_page), CancellationToken.None);

        result.Data!.Single().ResolvedAt.Should().BeNull();
    }
}
