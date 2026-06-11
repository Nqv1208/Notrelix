
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
    Guid? AutomationRuleId,
    Guid EventId,
    string EventType,
    ResourceType? ResourceType,
    Guid? ResourceId,
    AutomationExecutionStatus Status,
    int AttemptCount,
    string Payload,
    string? Response,
    string? Error,
    DateTime CreatedAt,
    DateTime? DeliveredAt,
    DateTime? FailedAt);
