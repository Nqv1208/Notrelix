namespace Notrelix.Application.Common.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency check fails (If-Match version mismatch).
/// Stable error code: common.precondition-failed
/// </summary>
public class PreconditionFailedException : Exception
{
    public string ErrorCode { get; }
    public long? ExpectedVersion { get; }
    public long? CurrentVersion { get; }

    /// <summary>Extra members of the public problem (for example the current state the caller lost the race to).</summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; init; } = new Dictionary<string, object?>();

    public PreconditionFailedException(
        string message,
        string errorCode = "common.precondition-failed",
        long? expectedVersion = null,
        long? currentVersion = null)
        : base(message)
    {
        ErrorCode = errorCode;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }
}
