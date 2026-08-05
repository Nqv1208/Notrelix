namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Origin of the raw idempotency execution key within the current scope.
/// The key itself lives only in <see cref="IdempotencyExecutionContext"/>.
/// </summary>
public enum IdempotencyExecutionSource
{
    Http,
    Message,
    Internal
}
