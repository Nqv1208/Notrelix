namespace Notrelix.Application.Common.Exceptions;

/// <summary>A well-formed request whose content a rule refuses (HTTP 422), with a stable public code.</summary>
public class UnprocessableException(string message, string errorCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
