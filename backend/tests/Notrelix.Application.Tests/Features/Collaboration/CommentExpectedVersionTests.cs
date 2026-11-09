using Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>Edit and delete quote the comment version the reader saw; the pipeline refuses a stale one with 412.</summary>
public class CommentExpectedVersionTests
{
    private readonly Guid _comment = Guid.CreateVersion7();

    [Fact]
    public void UpdateAndDelete_CarryTheCommentVersionAsTheirExpectedVersion()
    {
        var update = new UpdateCommentCommand(_comment, "Edited", 7);
        var delete = new DeleteCommentCommand(_comment, 7);

        update.Should().BeAssignableTo<IExpectedVersionRequest>();
        delete.Should().BeAssignableTo<IExpectedVersionRequest>();
        update.ExpectedVersion.Should().Be(7);
        delete.ExpectedVersion.Should().Be(7);
    }

    [Fact]
    public void UpdateAndDelete_DeclareTheCommentResourceTheVersionGuardBindsTo()
    {
        new UpdateCommentCommand(_comment, "Edited", 1).Resource.Kind.Value.Should().Be("collaboration.comment");
        new DeleteCommentCommand(_comment, 1).Resource.Kind.Value.Should().Be("collaboration.comment");
        new DeleteCommentCommand(_comment, 1).Resource.ResourceId.Should().Be(_comment);
    }
}
