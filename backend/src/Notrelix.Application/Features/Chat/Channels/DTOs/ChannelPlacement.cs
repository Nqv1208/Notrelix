namespace Notrelix.Application.Features.Chat.Channels.DTOs;

/// <summary>One channel's place in the sidebar, with the version the client last read for it.</summary>
public sealed record ChannelPlacement(Guid ChannelId, Guid? GroupId, double Position, long ExpectedVersion);
