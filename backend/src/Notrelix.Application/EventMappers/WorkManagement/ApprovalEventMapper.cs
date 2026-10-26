using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.WorkManagement.Approvals.Events;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the approval Domain facts a person must hear about to the producer-owned outward contracts:
/// an approver was named, and a request was decided.
/// </summary>
public sealed class ApprovalEventMapper :
    IntegrationEventMapperBase<ApprovalStepAssignedDomainEvent, ApprovalRequestedIntegrationEvent>,
    IIntegrationEventMapper<ApprovalRequestApprovedDomainEvent, ApprovalDecidedIntegrationEvent>,
    IIntegrationEventMapper<ApprovalRequestRejectedDomainEvent, ApprovalDecidedIntegrationEvent>
{
    public override ApprovalRequestedIntegrationEvent? Map(ApprovalStepAssignedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            RequestId: domainEvent.RequestId,
            TargetKind: domainEvent.Target.Kind.Value,
            TargetId: domainEvent.Target.ResourceId,
            ApproverUserId: domainEvent.ApproverUserId,
            RequestedBy: domainEvent.RequestedBy,
            Title: domainEvent.Title,
            Note: domainEvent.Note,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public ApprovalDecidedIntegrationEvent? Map(ApprovalRequestApprovedDomainEvent domainEvent) =>
        Decided(domainEvent, domainEvent.RequestId, domainEvent.RequestedBy, domainEvent.Target, domainEvent.DecidedBy, true, domainEvent.Note);

    public ApprovalDecidedIntegrationEvent? Map(ApprovalRequestRejectedDomainEvent domainEvent) =>
        Decided(domainEvent, domainEvent.RequestId, domainEvent.RequestedBy, domainEvent.Target, domainEvent.DecidedBy, false, domainEvent.Note);

    private static ApprovalDecidedIntegrationEvent? Decided(
        WorkspaceScopedDomainEvent domainEvent, Guid requestId, Guid? requestedBy, ResourceRef? target, Guid decidedBy, bool approved, string? note) =>
        requestedBy is null || target is null
            ? null
            : new(
                EventId: Guid.CreateVersion7(),
                AccountIdValue: domainEvent.AccountId,
                WorkspaceIdValue: domainEvent.WorkspaceId,
                RequestId: requestId,
                TargetKind: target.Kind.Value,
                TargetId: target.ResourceId,
                RequestedBy: requestedBy.Value,
                DecidedBy: decidedBy,
                Approved: approved,
                Note: note,
                CorrelationId: domainEvent.EventId,
                SourceEventId: domainEvent.EventId,
                OccurredAt: domainEvent.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        IIntegrationEvent? mapped = domainEvent switch
        {
            ApprovalStepAssignedDomainEvent assigned => Map(assigned),
            ApprovalRequestApprovedDomainEvent approved => Map(approved),
            ApprovalRequestRejectedDomainEvent rejected => Map(rejected),
            _ => null,
        };
        return mapped is null ? [] : [new IntegrationEventMapping(mapped)];
    }
}
