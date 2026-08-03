namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped, transport-neutral read side of the idempotency execution context.
/// The raw execution key is bound by a transport (HTTP endpoint filter,
/// typed message consumer) and consumed by the pipeline.
/// </summary>
public interface IIdempotencyExecutionContext
{
    /// <summary>
    /// Returns the raw execution key for the current scope.
    /// Throws <see cref="IdempotencyContextMissingException"/> when no transport
    /// has bound a key before an idempotent request is dispatched.
    /// </summary>
    string RequireKey();

    IdempotencyExecutionSource Source { get; }

    bool IsReplay { get; }
}
