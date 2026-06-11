namespace Notrelix.Domain.Governance.Security;

public enum SecurityEventType
{
    FailedLogin,
    SuspiciousLogin,
    PermissionDenied,
    DataExport,
    MfaBypassed
}
