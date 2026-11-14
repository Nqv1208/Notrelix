namespace Notrelix.Application.Features.Chat.Abstractions;

/// <summary>
/// Hands out the gap-free, monotonic sequence of a space (CHT-005). Both calls run inside the transaction that stores
/// the message, so a sequence is used only when its message is stored. The implementation takes a row lock on the
/// space, so senders to one space run one at a time while senders to other spaces do not wait.
/// </summary>
public interface ISpaceSequenceAllocator
{
    /// <summary>Locks the space row until the transaction ends. Call before reading for a duplicate, so a race between two sends cannot store two rows.</summary>
    Task LockAsync(Guid spaceId, CancellationToken cancellationToken);

    /// <summary>Advances the space's sequence by one and returns the new value (UPDATE ... RETURNING last_sequence).</summary>
    Task<long> NextAsync(Guid spaceId, CancellationToken cancellationToken);
}
