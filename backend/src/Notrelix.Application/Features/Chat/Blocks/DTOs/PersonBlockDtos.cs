namespace Notrelix.Application.Features.Chat.Blocks.DTOs;

/// <summary>The caller's block state of one person after a block change (UC-CHT-E06).</summary>
public sealed record PersonBlockDto(Guid UserId, bool Blocked);

/// <summary>One person the caller has blocked and when (UC-CHT-E06). Only the caller reads their own list.</summary>
public sealed record BlockedPersonDto(Guid UserId, DateTimeOffset BlockedAt);
