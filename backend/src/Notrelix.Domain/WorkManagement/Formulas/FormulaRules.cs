namespace Notrelix.Domain.WorkManagement.Formulas;

public static class FormulaRules
{
    public static void EnsureNoCircularDependency(Guid fieldId, IEnumerable<Guid> dependencies)
    {
        if (dependencies.Contains(fieldId))
            throw new DomainException("Circular dependency detected in formula.");
    }
}
