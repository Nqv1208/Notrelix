using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunCancelledDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
