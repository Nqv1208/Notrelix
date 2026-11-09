using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// UC-COL-H04: the create and update validators and the Domain share one content limit (Comment.MaxContentLength).
/// A body at the limit is accepted; one character more is refused with a validation error (400).
/// </summary>
public sealed class CommentContentLengthTests
{
    [Fact]
    public void TheLimitIsTheDocumentedTenThousandCharacters() =>
        Comment.MaxContentLength.Should().Be(10000);

    [Fact]
    public void Create_AcceptsABodyAtTheLimit()
    {
        var command = CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), new string('a', Comment.MaxContentLength), null);

        new CreateCommentCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_RefusesABodyOneCharacterOverTheLimit()
    {
        var command = CreateCommentCommand.ForBoardItem(Guid.CreateVersion7(), new string('a', Comment.MaxContentLength + 1), null);

        var result = new CreateCommentCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCommentCommand.ContentMd));
    }

    [Fact]
    public void Update_AcceptsABodyAtTheLimit()
    {
        var command = new UpdateCommentCommand(Guid.CreateVersion7(), new string('a', Comment.MaxContentLength), 1);

        new UpdateCommentCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_RefusesABodyOneCharacterOverTheLimit()
    {
        var command = new UpdateCommentCommand(Guid.CreateVersion7(), new string('a', Comment.MaxContentLength + 1), 1);

        var result = new UpdateCommentCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCommentCommand.ContentMd));
    }
}
