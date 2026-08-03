namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Thrown when an idempotent request is dispatched without a bound execution key.
/// A transport (HTTP endpoint filter or typed message consumer) must set the key
/// before dispatch.
/// </summary>
public sealed class IdempotencyContextMissingException : Exception
{
    public IdempotencyContextMissingException(string message) : base(message) { }
}
