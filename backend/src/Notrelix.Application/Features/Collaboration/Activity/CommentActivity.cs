using System.Text.Json;

namespace Notrelix.Application.Features.Collaboration.Activity;

/// <summary>
/// The Activity row a comment leaves. The row's title is the stable event key, never an English sentence; the label key
/// and the target type are structured parameters, which a client words in its own language. Activity is separate from
/// the comment, so editing or deleting a comment does not change these rows (product: Comment delete versus Activity).
/// </summary>
public static class CommentActivity
{
    public const string CommentCreated = "collab.comment-created";

    /// <summary>The localizable label key for an event key.</summary>
    public static string LabelKey(string eventKey) => $"activity.{eventKey}";

    /// <summary>The structured parameters of a row: the label key a client looks up and the kind of target the comment is on.</summary>
    public static string ParametersJson(string eventKey, string targetType) =>
        JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["labelKey"] = LabelKey(eventKey),
            ["targetType"] = targetType,
        });
}
