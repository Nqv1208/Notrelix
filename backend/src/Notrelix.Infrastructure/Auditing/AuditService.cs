using System.Text.Json;
using Notrelix.Domain.Common.Auditing;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Auditing;

internal sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IClientMetadata _client;
    private readonly IExecutionContextReader _execution;

    public AuditService(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IClientMetadata client,
        IExecutionContextReader execution)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _client = client;
        _execution = execution;
    }

    public async Task RecordAsync(
        Guid workspaceId,
        Guid actorId,
        string action,
        ResourceRef target,
        AuditMetadata metadata,
        AuditSeverity severity,
        string ipAddress = "",
        string userAgent = "",
        CancellationToken cancellationToken = default,
        AuditChange? change = null)
    {
        var now = _dateTimeProvider.UtcNow;
        var auditLog = new Data.Audit.AuditLog(
            workspaceId: workspaceId,
            actorUserId: actorId,
            actorType: "User",
            action: action,
            resourceType: target.Kind.Value,
            resourceId: target.ResourceId,
            subjectType: change?.SubjectType,
            subjectId: change?.SubjectId,
            severity: severity.ToString(),
            outcome: "Succeeded",
            ipAddress: FirstNonEmpty(ipAddress, metadata.IpAddress, _client.IpAddress),
            userAgent: FirstNonEmpty(userAgent, metadata.UserAgent, _client.UserAgent),
            requestId: metadata.TraceId,
            correlationId: _execution.CorrelationId.ToString(),
            causationId: _execution.CausationId?.ToString(),
            beforeJson: change?.Before is null ? null : JsonSerializer.SerializeToDocument(change.Before),
            afterJson: change?.After is null ? null : JsonSerializer.SerializeToDocument(change.After),
            metadataJson: null,
            occurredAt: now);

        _context.EnterpriseAuditLogs.Add(auditLog);
        await Task.CompletedTask;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
