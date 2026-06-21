using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Formulas;

public sealed class FormulaExpression : ValueObject
{
    public string Expression { get; }

    private FormulaExpression() { }    private FormulaExpression(string expression)
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
