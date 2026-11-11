namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// The fixed set of reaction kinds a person may react with (CHT-011, COL-009). It is the Collaboration palette: six emoji. Chat
/// keeps its own copy because the two contexts do not share Application types; a parity test holds the two lists equal.
/// </summary>
public static class ReactionKinds
{
    public static readonly IReadOnlyList<string> Palette = ["👍", "❤️", "😄", "🎉", "👀", "🚀"];

    public static bool Contains(string? kind) => kind is not null && Palette.Contains(kind);

    /// <summary>The position of a kind on the palette, for a stable display order. An unknown kind sorts last.</summary>
    public static int Rank(string kind)
    {
        for (var i = 0; i < Palette.Count; i++)
            if (Palette[i] == kind) return i;
        return int.MaxValue;
    }
}
