namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Base exception cho tất cả các lỗi domain
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception khi có lỗi validation trong domain
/// </summary>
public class DomainValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public DomainValidationException(string propertyName, string error)
        : base($"Validation failed for '{propertyName}': {error}")
    {
        Errors = new Dictionary<string, string[]> { { propertyName, new[] { error } } };
    }
}

/// <summary>
/// Exception khi vi phạm business rule
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
}
