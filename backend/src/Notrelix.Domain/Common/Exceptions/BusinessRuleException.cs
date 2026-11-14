namespace Notrelix.Domain.Common.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    private static readonly IReadOnlyDictionary<string, object?> NoDetails = new Dictionary<string, object?>();

    public string RuleCode { get; }

    /// <summary>
    /// Stable facts about the refusal a client can show in its own words (the fields of a cycle, the name of an
    /// unknown field, a position): copied onto the problem response next to <c>ruleCode</c>. Empty for most rules.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Details { get; }

    public BusinessRuleException(string ruleCode, string message) : this(ruleCode, message, NoDetails)
    {
    }

    public BusinessRuleException(string ruleCode, string message, IReadOnlyDictionary<string, object?> details) : base(message)
    {
        if (string.IsNullOrWhiteSpace(ruleCode))
            throw new ArgumentException("Rule code is required.", nameof(ruleCode));
        RuleCode = ruleCode;
        Details = details;
    }
}
