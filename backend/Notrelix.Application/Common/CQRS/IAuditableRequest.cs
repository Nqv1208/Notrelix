namespace Notrelix.Application.Common.CQRS;

public interface IAuditableRequest
{
    string AuditAction { get; }
    ResourceRef Resource { get; }
}
