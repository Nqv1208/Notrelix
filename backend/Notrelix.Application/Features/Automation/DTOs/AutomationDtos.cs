
namespace Notrelix.Application.Features.Automation.DTOs;

public record AutomationRuleDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record AutomationExecutionDto(
    Guid Id,
    Guid WorkspaceId,
    Guid RuleId,
    AutomationExecutionStatus Status,
    int AttemptCount,
    string? Payload,
    string? Error,
    DateTime CreatedAt);
