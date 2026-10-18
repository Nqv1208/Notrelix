namespace Notrelix.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException() : base("Conflict occurred.") { }
    public ConflictException(string message) : base(message) { }

    /// <param name="errorCode">Stable public error code; when absent the API reports the generic conflict code.</param>
    public ConflictException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public string? ErrorCode { get; }
}
