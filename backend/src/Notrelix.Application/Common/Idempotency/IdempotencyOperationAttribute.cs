namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Declares the stable operation identity for an idempotent request.
/// Convention: {context}.{module}.{command-name-without-Command-as-kebab}.vN
/// The value is an explicit literal — never derived from CLR type at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class IdempotencyOperationAttribute : Attribute
{
    public string Operation { get; }

    public IdempotencyOperationAttribute(string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        Operation = operation;
    }
}
