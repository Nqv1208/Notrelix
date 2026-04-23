namespace Notrelix.Domain.Enums;

// Mức quyền truy cập vào resource
public enum PermissionLevel
{
    None = 0,
    Viewer = 1,
    Commenter = 2,
    Editor = 3,
    Owner = 4
}
