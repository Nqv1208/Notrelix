using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// TAC-DC-002/003 — the pinned Collaboration mutation stays local: the comment
/// stores the foreign Work item as the canonical SharedKernel ResourceRef,
/// parent-comment validation is Collaboration-local, and no Work aggregate or
/// Work persistence participates anywhere in the handler.
/// </summary>
public class CreateCommentResourceRefTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICurrentRequestContext> _requestContextMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();

    public CreateCommentResourceRefTests()
    {
        _requestContextMock.Setup(c => c.UserId).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.RequireAccountId()).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.RequireWorkspaceId()).Returns(Guid.CreateVersion7());
        _requestContextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _clockMock.Setup(c => c.UtcNow).Returns(TestNow);
    }

    private (CreateCommentCommandHandler Handler, Mock<DbSet<Comment>> Comments, Mock<ICollaborationDbContext> ContextMock) CreateSut()
    {
        var comments = TestDbSet.Create<Comment>();
        var contextMock = new Mock<ICollaborationDbContext>();
        contextMock.Setup(c => c.Comments).Returns(comments.Object);
        return (new CreateCommentCommandHandler(contextMock.Object, _requestContextMock.Object, _clockMock.Object), comments, contextMock);
    }

    [Fact]
    public async Task ForBoardItem_StoresCanonicalResourceRef_ThroughCollaborationContextOnly()
    {
        var (sut, comments, _) = CreateSut();
        var boardItemId = Guid.CreateVersion7();

        var result = await sut.Handle(CreateCommentCommand.ForBoardItem(boardItemId, "hello", null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        comments.Verify(c => c.Add(It.Is<Comment>(comment =>
            comment.Target.Kind.Value == "work-management.board-item" &&
            comment.Target.ResourceId == boardItemId &&
            comment.Target.WorkspaceId == _requestContextMock.Object.RequireWorkspaceId())), Times.Once);
    }

    [Fact]
    public async Task Reply_WithUnknownParent_FailsWithoutForeignLookup()
    {
        var (sut, comments, _) = CreateSut();

        await sut.Invoking(s => s.Handle(
                CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), "orphan reply", Guid.CreateVersion7()),
                CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>(
                "parent-comment existence is a Collaboration-owned check and must not require the Work context");
        comments.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public void Command_CarriesStableResourceRefIdentity()
    {
        var boardItemId = Guid.CreateVersion7();
        var command = CreateCommentCommand.ForBoardItem(boardItemId, "hello", null);

        command.Resource.Kind.Value.Should().Be("work-management.board-item");
        command.Resource.ResourceId.Should().Be(boardItemId);
        command.Resource.WorkspaceId.Should().BeNull(
            "the request-level ref is the stable cross-resource identity; workspace scope is bound by the pipeline");
    }
}
