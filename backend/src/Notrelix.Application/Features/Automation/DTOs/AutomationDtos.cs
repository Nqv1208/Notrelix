
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
    string? TriggerConfiguration = null,
    long Version = 1,
    IReadOnlyList<AutomationActionDto>? Actions = null,
    string? Conditions = null);

/// <summary>One action of a rule, in the order it runs.</summary>
public record AutomationActionDto(string Type, string? Configuration);

/// <summary>What happened to one action of an execution.</summary>
public record AutomationExecutionStepDto(int Index, string Type, string Status, string? Error);

public record AutomationExecutionDto(
    Guid Id,
    Guid WorkspaceId,
    Guid RuleId,
    AutomationExecutionStatus Status,
    int AttemptCount,
    string? Payload,
    string? Error,
    DateTime CreatedAt,
    long? RuleVersion = null,
    IReadOnlyList<AutomationExecutionStepDto>? Steps = null);
