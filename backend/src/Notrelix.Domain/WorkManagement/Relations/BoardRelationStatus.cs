namespace Notrelix.Domain.WorkManagement.Relations;

public enum BoardRelationStatus
{
    Active = 0,
    Paused = 1,
    Broken = 2
    // 3 retired: Deleted — deletion is now tracked via IsDeleted, not business status
}
