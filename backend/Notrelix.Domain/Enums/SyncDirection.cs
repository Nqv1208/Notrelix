namespace Notrelix.Domain.Enums;

// Hướng đồng bộ calendar
public enum SyncDirection
{
    Push = 0,   // App → External
    Pull = 1,   // External → App
    Both = 2    // Hai chiều
}
