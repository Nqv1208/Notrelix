using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations;

public static class RelationRules
{
    public static void EnsureDifferentBoards(Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId)
            throw new DomainException("Source and target boards must be different for a relation.");
    }
}
