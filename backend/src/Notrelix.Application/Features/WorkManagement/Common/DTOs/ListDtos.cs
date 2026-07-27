namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record SavedFilterDto(
    Guid Id,
    string Name,
    Guid BoardId,
    Guid? ViewId,
    string Visibility,
    DateTimeOffset CreatedAt
);

public record ApprovalRequestDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    Guid RequestedByUserId,
    DateTimeOffset CreatedAt,
    List<ApprovalStepDto> Steps
);

public record ApprovalStepDto(
    Guid Id,
    Guid? ApproverUserId,
    Guid? ApproverTeamId,
    string Status,
    int Position,
    DateTimeOffset? DecidedAt,
    string? Note
);

public record FormDto(
    Guid Id,
    string Name,
    string Slug,
    Guid BoardId,
    string Status,
    string Visibility,
    DateTimeOffset CreatedAt
);

public record FormSubmissionDto(
    Guid Id,
    Guid FormId,
    Guid BoardId,
    Guid? CreatedItemId,
    Guid? SubmitterUserId,
    string? SubmitterEmail,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ProcessedAt
);
