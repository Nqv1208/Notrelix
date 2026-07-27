namespace Notrelix.Domain.Common.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public string RuleCode { get; }

    public BusinessRuleException(string ruleCode, string message) : base(message)
    {
        if (string.IsNullOrWhiteSpace(ruleCode))
            throw new ArgumentException("Rule code is required.", nameof(ruleCode));
        RuleCode = ruleCode;
    }
}
