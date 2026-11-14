namespace Notrelix.Domain.Chat.ReadStates;

/// <summary>
/// The read boundary rules (CHT-009, UC-CHT-D02, UC-CHT-D07). The boundary is the highest sequence a person has seen in a
/// space. It moves forward only and never passes the space's last sequence. The store applies the same rule in one
/// statement (GREATEST/LEAST on the membership row), so two concurrent mark-read calls cannot move it backwards.
/// </summary>
public static class ReadBoundary
{
    /// <summary>The sequence a mark-read request asks for, bounded by the last sequence of the space. A request above the last sequence reads the space to its end.</summary>
    public static long Clamp(long requested, long lastSequence)
    {
        if (requested < 0)
            throw new ArgumentOutOfRangeException(nameof(requested), requested, "A read boundary is a sequence of 0 or more.");
        if (lastSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(lastSequence), lastSequence, "A space's last sequence is 0 or more.");

        return Math.Min(requested, lastSequence);
    }

    /// <summary>The boundary after a request: the target when it is ahead of the current boundary, the current boundary otherwise.</summary>
    public static long Advance(long current, long target) => Math.Max(current, target);
}
