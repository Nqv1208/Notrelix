using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Rules;

public static class KanbanRules
{
    public static void EnsureValidColumnField(Guid fieldId)
    {
        Guard.NotEmpty(fieldId);
    }
}
