namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// The Notrelix-owned stickers (UC-CHT-I06, SPEC-CHAT-MEDIA-CALLS). Only these exist: a Workspace cannot upload one and there is no
/// third-party GIF provider. A sticker message stores the id; the art is drawn by the client from the id, so a client that does not know an
/// id still reads its label. Adding a sticker is adding a row here and its art in the client; an id is never reused or removed.
/// </summary>
public static class StickerCatalog
{
    /// <summary>The sticker ids with their English label, in the order the picker shows them.</summary>
    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["thumbs-up"] = "Thumbs up",
        ["clap"] = "Clapping",
        ["party"] = "Party",
        ["heart"] = "Heart",
        ["laugh"] = "Laughing",
        ["wow"] = "Wow",
        ["thinking"] = "Thinking",
        ["sad"] = "Sad",
        ["fire"] = "On fire",
        ["rocket"] = "Rocket",
        ["eyes"] = "Eyes on it",
        ["check"] = "Done",
        ["pray"] = "Thank you",
        ["hundred"] = "100",
        ["coffee"] = "Coffee break",
        ["bug"] = "Bug",
    };

    public static IReadOnlyCollection<string> Ids => Labels.Keys.ToList();

    public static bool Exists(string? id) => id is not null && Labels.ContainsKey(id);

    /// <summary>The text a sticker message carries for previews, notifications and search: the label, never user-typed text.</summary>
    public static string PreviewText(string id) =>
        Labels.TryGetValue(id, out var label) ? $"Sticker: {label}" : throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown sticker.");
}
