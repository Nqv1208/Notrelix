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

    public long MaxBytes { get; }
}
