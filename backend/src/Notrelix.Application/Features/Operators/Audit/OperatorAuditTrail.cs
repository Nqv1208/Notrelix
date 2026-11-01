using System.Text.Json;
using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Application.Features.Operators.Public.Audit;
using Notrelix.Application.Features.Operators.Public.Principal;
using Notrelix.Domain.Operators;

namespace Notrelix.Application.Features.Operators.Audit;

internal sealed class OperatorAuditTrail : IOperatorAuditTrail
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IOperatorsDbContext _context;
    private readonly ICurrentOperator _operator;
    private readonly IDateTimeProvider _clock;

    public OperatorAuditTrail(IOperatorsDbContext context, ICurrentOperator currentOperator, IDateTimeProvider clock)
    {
        _context = context;
        _operator = currentOperator;
        _clock = clock;
    }

    public void Record(OperatorAuditRecord record) =>
        _context.OperatorAudit.Add(OperatorAuditEntry.Record(
            _operator.OperatorId,
            record.Action,
            record.TargetKind,
            record.TargetId,
            record.AccountId,
            record.Reason,
            record.Before is null ? null : JsonSerializer.Serialize(record.Before, Json),
            record.After is null ? null : JsonSerializer.Serialize(record.After, Json),
            _clock.UtcNow));
}
