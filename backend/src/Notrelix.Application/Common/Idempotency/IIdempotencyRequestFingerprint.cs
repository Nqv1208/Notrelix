namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Computes a deterministic fingerprint for an idempotent request.
/// The fingerprint captures business identity only — transport/correlation metadata is excluded.
/// </summary>
public interface IIdempotencyRequestFingerprint
{
    string Compute(IIdempotentRequest request, Type requestType);
}
