namespace Notrelix.Application.Features.Chat.Blocks.DTOs;

/// <summary>The caller's block state of one person after a block change (UC-CHT-E06).</summary>
public sealed record PersonBlockDto(Guid UserId, bool Blocked);
