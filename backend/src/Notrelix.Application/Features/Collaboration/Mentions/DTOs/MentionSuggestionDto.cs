namespace Notrelix.Application.Features.Collaboration.Mentions.DTOs;

/// <summary>A person the caller may @-mention on a page or board item: who they are, nothing about their standing.</summary>
public sealed record MentionSuggestionDto(Guid UserId, string Name, string? AvatarUrl);
