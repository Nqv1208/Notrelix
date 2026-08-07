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
