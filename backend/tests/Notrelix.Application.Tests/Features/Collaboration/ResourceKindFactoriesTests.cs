using Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// Spec 4.3: use-case-fixed resource kinds are owned by the Application factory
/// and never forwarded through the API endpoint.
/// </summary>
public class ResourceKindFactoriesTests
{
    [Fact]
    public void CreateComment_ForBoardItem_SetsBoardItemKind()
    {
        var command = CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "hello", null);

        command.ResourceKind.Value.Should().Be("work-management.board-item");
    }

    [Fact]
    public void CreateComment_ForPage_SetsPageKind()
    {
        var command = CreateCommentCommand.ForPage(Guid.NewGuid(), "hello", null);

        command.ResourceKind.Value.Should().Be("documents.page");
    }

    [Fact]
    public void GetComments_ForBoardItem_SetsBoardItemKind()
    {
        var query = GetCommentsQuery.ForBoardItem(Guid.NewGuid());

        query.ResourceKind.Value.Should().Be("work-management.board-item");
    }

    [Fact]
    public void GetComments_ForPage_SetsPageKind()
    {
        var query = GetCommentsQuery.ForPage(Guid.NewGuid());

        query.ResourceKind.Value.Should().Be("documents.page");
    }

    [Fact]
    public void GetResourceActivity_ForBoardItem_SetsBoardItemKind()
    {
        var query = GetResourceActivityQuery.ForBoardItem(Guid.NewGuid());

        query.ResourceKind.Value.Should().Be("work-management.board-item");
    }
}
