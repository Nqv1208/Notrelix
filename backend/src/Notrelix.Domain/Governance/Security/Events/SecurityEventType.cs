namespace Notrelix.Domain.Governance.Security.Events;

public enum SecurityEventType
{
    FailedLogin,
    SuspiciousLogin,
    PermissionDenied,
    DataExport,
    MfaBypassed
}
