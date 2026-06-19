using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunFailedDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);
