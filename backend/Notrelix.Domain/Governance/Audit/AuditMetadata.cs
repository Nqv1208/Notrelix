using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Audit;

public sealed class AuditMetadata : ValueObject
{
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public string? TraceId { get; }

    private AuditMetadata(string? ip, string? ua, string? traceId)
    {
        IpAddress = ip;
        UserAgent = ua;
        TraceId = traceId;
    }

    public static AuditMetadata Create(string? ip = null, string? ua = null, string? traceId = null)
    {
        return new AuditMetadata(ip, ua, traceId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IpAddress;
        yield return UserAgent;
        yield return TraceId;
    }
}
