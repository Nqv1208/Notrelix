namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped, transport-neutral execution context that carries the raw idempotency
/// key for the current request. One instance is registered as both
/// <see cref="IIdempotencyExecutionContext"/> and
/// <see cref="IIdempotencyExecutionContextWriter"/> per scope.
///
/// The implementation contains no ASP.NET or message-transport type. The raw key
/// is held only in memory for the scope, is hashed before persistence and is
/// never logged.
/// </summary>
public sealed class IdempotencyExecutionContext :
    IIdempotencyExecutionContext,
    IIdempotencyExecutionContextWriter
{
    public const int MinKeyLength = 8;
    public const int MaxKeyLength = 128;

    private string? _key;

    public IdempotencyExecutionSource Source { get; private set; } = IdempotencyExecutionSource.Internal;

    public bool IsReplay { get; private set; }

    public string RequireKey()
    {
        return _key
            ?? throw new IdempotencyContextMissingException(
                "No idempotency execution key is set in the current scope. " +
                "A transport must bind a key before an idempotent request is dispatched.");
    }

    public void Set(string key, IdempotencyExecutionSource source)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length is < MinKeyLength or > MaxKeyLength)
        {
            throw new ArgumentException(
                $"Idempotency key must be between {MinKeyLength} and {MaxKeyLength} characters.",
                nameof(key));
        }

        if (key.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Idempotency key must not contain control characters.",
                nameof(key));
        }

        if (key != key.Trim())
        {
            throw new ArgumentException(
                "Idempotency key must not have leading or trailing whitespace.",
                nameof(key));
        }

        if (_key is not null && _key != key)
        {
            throw new InvalidOperationException(
                "Cannot set two different idempotency keys in the same execution scope.");
        }

        _key = key;
        Source = source;
    }

    public void MarkReplay()
    {
        IsReplay = true;
    }
}
