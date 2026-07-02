namespace Notrelix.Application.Common.Messaging;

/// <summary>
/// Marker for integration commands — requests a specific bounded context to perform an action.
/// Unlike events (which announce something happened), commands request action.
/// Examples: ProvisionPersonalWorkspace, CreateBillingCustomer, IndexSearchDocument.
/// </summary>
public interface IIntegrationCommand
{
    Guid CommandId { get; }
    DateTimeOffset OccurredAt { get; }
}