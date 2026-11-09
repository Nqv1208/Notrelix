using System.Text.Json;
using Notrelix.Application.Features.Collaboration.Activity;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>A comment's Activity row carries a stable event key and structured parameters, never an English sentence.</summary>
public class CommentActivityTests
{
    [Fact]
    public void CommentCreated_HasAStableEventKeyAndALabelKey()
    {
        CommentActivity.CommentCreated.Should().Be("collab.comment-created");
        CommentActivity.LabelKey(CommentActivity.CommentCreated).Should().Be("activity.collab.comment-created");
    }

    [Fact]
    public void Parameters_NameTheLabelKeyAndTheTargetType()
    {
        var parameters = JsonDocument.Parse(CommentActivity.ParametersJson(CommentActivity.CommentCreated, "board-item")).RootElement;

        parameters.GetProperty("labelKey").GetString().Should().Be("activity.collab.comment-created");
        parameters.GetProperty("targetType").GetString().Should().Be("board-item");
    }
}
