namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>One filter condition: a field id (a board field, or a built-in "$title", "$label", "$assignee"), an operator and its operands.</summary>
public sealed record CriteriaCondition(string FieldId, string Operator, IReadOnlyList<string> Values);

public sealed record CriteriaSortLevel(string FieldId, bool Descending);

/// <summary>
/// What a member filters and sorts a board's Items by, in the wire form the web client already keeps in the URL: the
/// compound filter <c>c2:and~field.op.value~...</c> and the sort <c>field.asc~field.desc</c>. Parts are
/// percent-encoded so "." and "~" stay separators. The same criteria drive the paged read and its filtered totals.
/// </summary>
public sealed record BoardItemCriteria(bool MatchAll, IReadOnlyList<CriteriaCondition> Conditions, IReadOnlyList<CriteriaSortLevel> Sorts)
{
    public const int MaxSortLevels = 3;
    public const int MaxConditions = 20;
    public const int MaxLength = 4000;

    public static readonly BoardItemCriteria None = new(true, [], []);

    public bool HasFilter => Conditions.Count > 0;

    public static BoardItemCriteria Parse(string? filter, string? sort)
    {
        if (string.IsNullOrWhiteSpace(filter) && string.IsNullOrWhiteSpace(sort)) return None;
        if ((filter?.Length ?? 0) > MaxLength) throw Refuse("filter", "The filter is too long.");
        if ((sort?.Length ?? 0) > MaxLength) throw Refuse("sort", "The sort is too long.");

        var matchAll = true;
        var conditions = new List<CriteriaCondition>();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            const string prefix = "c2:";
            if (!filter.StartsWith(prefix, StringComparison.Ordinal)) throw Refuse("filter", "The filter is not a compound filter.");
            var parts = filter[prefix.Length..].Split('~');
            matchAll = parts[0] != "or";
            foreach (var raw in parts.Skip(1))
            {
                var pieces = raw.Split('.').Select(Decode).ToList();
                if (pieces.Count < 2 || pieces.Any(p => p is null) || pieces[0]!.Length == 0)
                {
                    throw Refuse("filter", "A filter condition is malformed.");
                }

                conditions.Add(new CriteriaCondition(pieces[0]!, pieces[1]!, pieces.Skip(2).Select(p => p!).ToList()));
            }

            if (conditions.Count > MaxConditions) throw Refuse("filter", $"A filter has at most {MaxConditions} conditions.");
        }

        var sorts = new List<CriteriaSortLevel>();
        if (!string.IsNullOrWhiteSpace(sort))
        {
            foreach (var raw in sort.Split('~'))
            {
                var pieces = raw.Split('.').Select(Decode).ToList();
                if (pieces.Count != 2 || pieces[0] is not { Length: > 0 } id || pieces[1] is not ("asc" or "desc"))
                {
                    throw Refuse("sort", "A sort level is malformed.");
                }

                sorts.Add(new CriteriaSortLevel(id, pieces[1] == "desc"));
            }

            if (sorts.Count > MaxSortLevels) throw Refuse("sort", $"A sort has at most {MaxSortLevels} levels.");
        }

        return new BoardItemCriteria(matchAll, conditions, sorts);
    }

    public static Notrelix.Application.Common.Exceptions.ValidationException Refuse(string property, string message) =>
        new([new ValidationFailure(property, message)]);

    private static string? Decode(string part)
    {
        try { return Uri.UnescapeDataString(part); }
        catch (UriFormatException) { return null; }
    }
}

/// <summary>How a Person field reads its people, as the web client does.</summary>
public enum PersonSource
{
    /// <summary>The value the field stores (a bare id or a list); no fallback.</summary>
    Stored,
    /// <summary>The Item's own people (the field mirrors its assignees).</summary>
    Members,
    /// <summary>The stored value when it is a non-empty list, else the Item's people.</summary>
    StoredOrMembers,
}

/// <summary>What the criteria need to know about one board field.</summary>
/// <param name="IsDueField">The board's first Date field: an Item's due date stands in when it holds no value of its own.</param>
/// <param name="MirrorsDue">A field named like a due date mirrors the Item's due date instead of holding a value.</param>
/// <param name="OptionOrder">Option ids in board order (Status, Select): a sort puts options by this order.</param>
public sealed record CriteriaField(
    Guid Id, FieldType Type, bool IsDueField, bool MirrorsDue, PersonSource PersonSource, IReadOnlyList<Guid> OptionOrder);

/// <summary>
/// One lane's page of Item ids with its sizes before and after the filter. A lane is what a Status field groups the
/// board into: one per option, and the empty lane ("" key) for Items whose status is none or no longer an option.
/// </summary>
/// <param name="NextCursor">Where the lane's next page starts; null when this page holds the rest.</param>
public sealed record BoardItemLanePage(string LaneKey, IReadOnlyList<Guid> ItemIds, int Total, int FilteredTotal, string? NextCursor);

/// <summary>
/// Reads the ordered ids of a page of each lane's live root Items under criteria, and the lane sizes. The
/// provider-specific query lives in Infrastructure; it reads through the caller's row-level-security scope and
/// refuses a field or operator it cannot evaluate with a validation failure (never a silent mismatch).
/// </summary>
public interface IBoardItemPageReader
{
    /// <param name="laneField">The Status field the board is laid out by; null puts every Item in the one empty lane.</param>
    /// <param name="onlyLane">A single lane to read; null reads every lane (an option per lane, then the empty lane).</param>
    Task<IReadOnlyList<BoardItemLanePage>> ReadAsync(
        Guid boardId,
        IReadOnlyList<Guid> groupIds,
        CriteriaField? laneField,
        IReadOnlyList<CriteriaField> fields,
        BoardItemCriteria criteria,
        string? onlyLane,
        string? cursor,
        int limit,
        CancellationToken cancellationToken);
}

/// <summary>Where a lane page stopped: the sort keys, group place, place and id of its last Item.</summary>
/// <param name="Sort">The sort the keys belong to; a cursor is refused under another sort.</param>
public sealed record LaneKeyset(string Sort, IReadOnlyList<string?> Keys, string GroupPosition, string Position, Guid Id);

/// <summary>
/// An opaque page cursor: the position of the last Item of a page in the lane's order (sort keys, then group place,
/// place and id). The next page is what follows that position, so a write on another Item cannot shift a page.
/// </summary>
public static class PageCursor
{
    private sealed record Wire(int V, string S, string?[] K, string G, string P, Guid I);

    public static string Encode(LaneKeyset keyset) =>
        Convert.ToBase64String(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                new Wire(2, keyset.Sort, keyset.Keys.ToArray(), keyset.GroupPosition, keyset.Position, keyset.Id)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static LaneKeyset? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var wire = System.Text.Json.JsonSerializer.Deserialize<Wire>(Convert.FromBase64String(padded));
            if (wire is { V: 2, S: not null, K: not null, G: not null, P: not null }) return new LaneKeyset(wire.S, wire.K, wire.G, wire.P, wire.I);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException) { }

        throw BoardItemCriteria.Refuse("cursor", "The cursor is not valid.");
    }

    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static int ClampLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
}
