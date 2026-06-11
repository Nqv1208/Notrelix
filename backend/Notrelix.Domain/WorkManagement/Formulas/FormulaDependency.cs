using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Formulas;

public class FormulaDependency : Entity
{
    public Guid FormulaFieldId { get; private set; }
    public Guid DependsOnFieldId { get; private set; }

    private FormulaDependency() : base() { }

    public static FormulaDependency Create(Guid formulaFieldId, Guid dependsOnFieldId)
    {
        Guard.NotEmpty(formulaFieldId);
        Guard.NotEmpty(dependsOnFieldId);

        return new FormulaDependency
        {
            FormulaFieldId = formulaFieldId,
            DependsOnFieldId = dependsOnFieldId
        };
    }
}
