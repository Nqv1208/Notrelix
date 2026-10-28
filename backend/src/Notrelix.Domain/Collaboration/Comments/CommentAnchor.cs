namespace Notrelix.Domain.Collaboration.Comments;

/// <summary>
/// Where in the target content a comment points. Collaboration owns the locator; the target's context owns the
/// content it points into, so an anchor never becomes content ownership and may go stale when that content changes.
/// A text-range anchor names a block of the target, the quoted text and where that text started and ended in the
/// block's plain text (UTF-16 code units, hard line breaks as "\n").
/// </summary>
public sealed class CommentAnchor : ValueObject
{
    public const int MaxQuoteLength = 500;
    public const int MaxOffset = 20_000;

    public string? Selector { get; }
    public int? Offset { get; }
    public Guid? BlockId { get; }
    public string? Quote { get; }
    public int? StartOffset { get; }
    public int? EndOffset { get; }

    /// <summary>True when the anchor names a quoted range inside one block.</summary>
    public bool IsTextRange => BlockId.HasValue;

    private CommentAnchor() { }
    private CommentAnchor(string? selector, int? offset, Guid? blockId = null, string? quote = null, int? startOffset = null, int? endOffset = null)
    {
        Selector = selector;
        Offset = offset;
        BlockId = blockId;
        Quote = quote;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public static CommentAnchor Create(string? selector = null, int? offset = null)
    {
        return new CommentAnchor(selector, offset);
    }

    public static CommentAnchor None() => new(null, null);

    /// <summary>
    /// A quoted range of one block. The quote is the exact text between the offsets, so the range and the text
    /// it claims cannot disagree. Whether the block belongs to the target is an external fact the caller checks.
    /// </summary>
    public static CommentAnchor ForTextRange(Guid blockId, string quote, int startOffset, int endOffset)
    {
        if (blockId == Guid.Empty)
            throw Invalid("The anchor must name a block.");
        if (string.IsNullOrWhiteSpace(quote))
            throw Invalid("The anchor must quote some text.");
        if (quote.Length > MaxQuoteLength)
            throw Invalid($"The quoted text cannot exceed {MaxQuoteLength} characters.");
        if (startOffset < 0 || endOffset <= startOffset || endOffset > MaxOffset)
            throw Invalid("The anchor range is not valid.");
        if (endOffset - startOffset != quote.Length)
            throw Invalid("The anchor range must be as long as the quoted text.");

        return new CommentAnchor(null, null, blockId, quote, startOffset, endOffset);
    }

    private static BusinessRuleException Invalid(string message) =>
        new(CollaborationRuleCodes.Collaboration_Comment_AnchorInvalid, message);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Selector;
        yield return Offset;
        yield return BlockId;
        yield return Quote;
        yield return StartOffset;
        yield return EndOffset;
    }
}
