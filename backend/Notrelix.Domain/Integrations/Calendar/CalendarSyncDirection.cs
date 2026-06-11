namespace Notrelix.Domain.Integrations.Calendar;

// Hướng đồng bộ calendar
public enum CalendarSyncDirection
{
    Push = 0,   // App → External
    Pull = 1,   // External → App
    Both = 2    // Hai chiều
}
