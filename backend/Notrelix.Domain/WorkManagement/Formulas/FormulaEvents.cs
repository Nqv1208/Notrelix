using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Formulas;

public record FormulaDependencyChangedEvent(Guid FormulaFieldId, Guid DependsOnFieldId) : DomainRecordEvent;
