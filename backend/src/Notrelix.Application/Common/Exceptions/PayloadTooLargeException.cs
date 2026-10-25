namespace Notrelix.Application.Common.Exceptions;

/// <summary>
/// The request body exceeded the size cap of the operation. Raised while the body is
/// being streamed, so no object is left behind. Public status: 413.
/// </summary>
public sealed class PayloadTooLargeException : Exception
{
    public PayloadTooLargeException(long maxBytes)
        : base($"The upload exceeds the maximum allowed size of {maxBytes} bytes.")
    {
        MaxBytes = maxBytes;
    }

    /// <param name="errorCode">Stable public error code; when absent the API reports the attachment code.</param>
    public PayloadTooLargeException(long maxBytes, string errorCode) : this(maxBytes)
    {
        ErrorCode = errorCode;
    }

    public long MaxBytes { get; }

    public string? ErrorCode { get; }
}
