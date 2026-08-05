namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Thrown when an idempotency key is reused with a different business payload
/// than the one originally executed under that key. The API maps this typed
/// exception to a 409 ProblemDetails — no exception-message parsing anywhere.
/// </summary>
public sealed class IdempotencyPayloadMismatchException : ConflictException
{
    public IdempotencyPayloadMismatchException(string operation)
        : base(
            $"The idempotency key was already used with a different request payload for operation '{operation}'. " +
            "Use a new key for a different operation.")
    {
        Operation = operation;
    }

    public string Operation { get; }
}
