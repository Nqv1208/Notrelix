namespace Notrelix.Application.Common.Auditing;

public interface IAuditableRequest
{
    string AuditAction { get; }
    ResourceRef Resource { get; }
}
