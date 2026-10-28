using System.Text.Json;

namespace Notrelix.Domain.Documents.Rules;

/// <summary>
/// The people a block mentions: every <c>mention</c> mark in its content and properties (text runs, image
/// captions, table cells), read from the stored JSON so each place rich text can appear is covered by one walk.
/// </summary>
public static class BlockMentions
{
    public static IReadOnlySet<Guid> Of(Blocks.Block block) => Of(block.Content?.Data.Value, block.Properties?.Data.Value);

    public static IReadOnlySet<Guid> Of(params string?[] documents)
    {
        var found = new HashSet<Guid>();
        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document)) continue;
            try
            {
                using var parsed = JsonDocument.Parse(document);
                Walk(parsed.RootElement, found);
            }
            catch (JsonException)
            {
                // Stored content is validated on write; an unreadable value mentions nobody.
            }
        }

        return found;
    }

    private static void Walk(JsonElement element, HashSet<Guid> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) Walk(item, found);
                break;
            case JsonValueKind.Object:
                if (element.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String
                    && type.GetString() == RichText.Mention
                    && element.TryGetProperty("userId", out var user)
                    && user.ValueKind == JsonValueKind.String
                    && Guid.TryParse(user.GetString(), out var userId)
                    && userId != Guid.Empty)
                {
                    found.Add(userId);
                }

                foreach (var member in element.EnumerateObject()) Walk(member.Value, found);
                break;
        }
    }
}
