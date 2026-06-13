using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunQueuedEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);

public sealed record AiAgentRunStartedEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);

public sealed record AiAgentRunSucceededEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);

public sealed record AiAgentRunFailedEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);

public sealed record AiAgentRunCancelledEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
