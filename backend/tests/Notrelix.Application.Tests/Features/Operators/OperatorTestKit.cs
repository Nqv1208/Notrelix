using Notrelix.Application.Features.Operators.Public.Audit;
using Notrelix.Application.Features.Operators.Public.Principal;

namespace Notrelix.Application.Tests.Features.Operators;

internal sealed class FakeOperator : ICurrentOperator
{
    public static readonly Guid Id = Guid.CreateVersion7();
    public Guid OperatorId => Id;
    public string Email => "ops@notrelix.test";
    public string DisplayName => "Ops";
}

/// <summary>Collects what the operator audit trail was asked to record, without a database.</summary>
internal sealed class RecordingAudit : IOperatorAuditTrail
{
    public List<OperatorAuditRecord> Records { get; } = [];

    public void Record(OperatorAuditRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Reason))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException("Operators_Audit_ReasonRequired", "A reason is required.");
        Records.Add(record);
    }
}

internal sealed class CountingCommit : IOperatorChangeCommit
{
    public int Commits { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Commits++;
        return Task.CompletedTask;
    }
}
