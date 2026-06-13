using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentCreatedEvent(
    Guid WorkspaceId,
    Guid AgentId,
    string Name,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);

public sealed record AiAgentUpdatedEvent(
    Guid WorkspaceId,
    Guid AgentId,
    string Name,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);

public sealed record AiAgentStatusChangedEvent(
    Guid WorkspaceId,
    Guid AgentId,
    AiAgentStatus Status,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
