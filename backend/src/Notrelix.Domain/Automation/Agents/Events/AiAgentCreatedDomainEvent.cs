using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentCreatedDomainEvent(
    Guid WorkspaceId,
    Guid AgentId,
    string Name,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
