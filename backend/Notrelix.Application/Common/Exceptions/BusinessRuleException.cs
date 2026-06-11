namespace Notrelix.Application.Common.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException() : base("Business rule violated.") { }
    public BusinessRuleException(string message) : base(message) { }
}
