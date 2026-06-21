namespace Notrelix.Testing.Core;

public static class TestIds
{
    public static Guid NewId() => Guid.NewGuid();
    public static Guid NewWorkspaceId() => Guid.NewGuid();
    public static Guid NewBoardId() => Guid.NewGuid();
    public static Guid NewUserId() => Guid.NewGuid();
    public static Guid NewTeamId() => Guid.NewGuid();
    public static Guid NewSpaceId() => Guid.NewGuid();
    public static Guid NewFieldId() => Guid.NewGuid();
    public static Guid NewItemId() => Guid.NewGuid();
}
