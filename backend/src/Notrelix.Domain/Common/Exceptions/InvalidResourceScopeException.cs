namespace Notrelix.Domain.Common.Exceptions;

public class InvalidResourceScopeException : DomainException
{
    public InvalidResourceScopeException(string message) : base(message) { }
}
