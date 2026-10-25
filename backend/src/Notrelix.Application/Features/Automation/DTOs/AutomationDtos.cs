
namespace Notrelix.Application.Features.Automation.DTOs;

public record AutomationRuleDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string TriggerType,
    string ActionType,
    string Configuration,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? ExecutorUserId = null,
    string? DisabledReason = null,
    Guid? BoardId = null,
    string? TriggerConfiguration = null);

public record AutomationExecutionDto(
    Guid Id,
    Guid WorkspaceId,
    Guid RuleId,
    AutomationExecutionStatus Status,
    int AttemptCount,
    string? Payload,
    string? Error,
    DateTime CreatedAt);
