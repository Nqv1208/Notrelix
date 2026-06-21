namespace Notrelix.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Forbidden access.") { }
    public ForbiddenException(string message) : base(message) { }
}
