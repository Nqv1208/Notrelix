namespace Notrelix.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Forbidden access.") { }
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string message, string? code) : base(message)
    {
        Code = code;
    }

    /// <summary>A stable denial reason (see AccessDenialCodes); null for a plain permission denial.</summary>
    public string? Code { get; }
}
