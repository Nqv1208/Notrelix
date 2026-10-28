namespace Notrelix.Application.Features.Collaboration.Reactions;

/// <summary>The fixed set of emoji a person may react with.</summary>
public static class ReactionPalette
{
    public static readonly IReadOnlyList<string> Emojis = ["👍", "❤️", "😄", "🎉", "👀", "🚀"];

    public static bool Contains(string? emoji) => emoji is not null && Emojis.Contains(emoji);
}
