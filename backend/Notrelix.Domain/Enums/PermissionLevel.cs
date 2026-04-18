namespace Notrelix.Domain.Enums;

// Mức độ quyền truy cập
public enum PermissionLevel
{
    None = 0,
    Viewer = 1,
    Read = 1,
    Commenter = 2,
    Editor = 3,
    Write = 3,
    Owner = 4,
    Admin = 4
}
