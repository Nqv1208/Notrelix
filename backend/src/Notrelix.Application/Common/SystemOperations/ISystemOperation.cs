namespace Notrelix.Application.Common.SystemOperations;

public interface ISystemOperation
{
    string OperationName { get; }
    SystemOperationReason Reason { get; }
    Guid CorrelationId { get; }
}
