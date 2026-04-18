namespace Notrelix.Application.Common.Exceptions;

// Exception khi authentication thất bại
public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Unauthorized") { }
    public UnauthorizedException(string message) : base(message) { }
}
