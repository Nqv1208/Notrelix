using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunQueuedDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);
