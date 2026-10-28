namespace Notrelix.Application.Features.Collaboration.Activity.DTOs;

/// <param name="Action">An English phrase for the fact (without the actor), for clients that do not know <paramref name="Type"/>.</param>
/// <param name="Type">The stable type of the fact, e.g. <c>work.item-status-changed</c>; clients word it in their language.</param>
/// <param name="Kind"><c>created</c> for the fact that the resource came into being; otherwise null.</param>
/// <param name="Change">The small language-neutral detail of the fact: a field, a before and after, a person.</param>
public record ActivityLogDto(
    Guid Id,
    Guid ActorId,
    string? ActorName,
    string Action,
    string ResourceKind,
    Guid ResourceId,
    string? ResourceTitle,
    DateTime CreatedAt,
    string? ActorAvatarUrl = null,
    string? Type = null,
    string? Kind = null,
    ActivityChangeDto? Change = null
);

/// <summary>What changed, without sentences: names and short values only, never a document body or a secret.</summary>
public record ActivityChangeDto(
    string? FieldName,
    string? From,
    string? To,
    Guid? PersonId,
    string? PersonName,
    string? ItemKey
);

public record ResourceActivityPageDto(
    List<ActivityLogDto> Data,
    int Total,
    int Page,
    int PageSize
);
