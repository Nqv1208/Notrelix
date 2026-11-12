using Notrelix.Application.Features.Chat.Messages.DTOs;

namespace Notrelix.Application.Features.Chat.Pins.DTOs;

/// <summary>A pinned message as the space's readers see it, with who pinned it and when (UC-CHT-F06, F07).</summary>
public sealed record PinDto(MessageDto Message, Guid PinnedBy, DateTimeOffset PinnedAt);

/// <summary>The pin and whether this request made it (false when the message was already pinned).</summary>
public sealed record PinResultDto(PinDto Pin, bool Created);

/// <summary>A page of pins, newest pin first. NextCursor is null on the last page.</summary>
public sealed record PinPageDto(IReadOnlyList<PinDto> Items, string? NextCursor);
