namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Thrown when the idempotency store observes corrupt or legacy incomplete state:
/// a committed Processing record that is still active, or a Completed record
/// without a stored result (spec 3.8). Processing is never returned as Completed.
/// The current request rolls back; the API maps this to 503 + Retry-After.
/// </summary>
public sealed class IdempotencyIncompleteStateException : Exception
{
    public IdempotencyIncompleteStateException(string operation)
        : base(
            $"The idempotency state for operation '{operation}' is incomplete. " +
            "The operation is being processed or its stored state is corrupt. Retry later.")
    {
        Operation = operation;
    }

    public string Operation { get; }
}
