namespace Notrelix.Domain.WorkManagement.Formulas;

/// <summary>
/// Experimental formula expression value object. Parser/evaluator is not yet
/// implemented, so production code must not depend on this type. Promotion
/// requires an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36; isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public sealed class FormulaExpression : ValueObject
{
    public string Expression { get; } = null!;

    private FormulaExpression() { }
    private FormulaExpression(string expression)
    {
        Expression = expression;
    }

    public static FormulaExpression Create(string expression)
    {
        Guard.NotNullOrWhiteSpace(expression);
        return new FormulaExpression(expression.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Expression;
    }
}
