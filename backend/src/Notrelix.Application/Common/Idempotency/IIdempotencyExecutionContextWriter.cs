namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped write side of the idempotency execution context.
/// Bound by transports before dispatch and by the pipeline when a replay occurs.
/// </summary>
public interface IIdempotencyExecutionContextWriter
{
    /// <summary>
    /// Binds the raw execution key for the current scope.
    /// Rejects keys outside the 8-128 character range, control characters and
    /// leading/trailing whitespace. A scope cannot hold two different keys.
    /// </summary>
    void Set(string key, IdempotencyExecutionSource source);

    /// <summary>
    /// Marks the current execution as an idempotent replay so the transport can
    /// surface it (e.g. an HTTP replay header).
    /// </summary>
    void MarkReplay();
}
