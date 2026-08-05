namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Marks a property to be excluded from the idempotency request fingerprint.
/// Use for correlation/trace/transport metadata that is not part of business identity.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class IdempotencyFingerprintIgnoreAttribute : Attribute;
