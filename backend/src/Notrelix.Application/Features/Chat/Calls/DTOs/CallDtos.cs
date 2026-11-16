using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Application.Features.Chat.Calls.DTOs;

public sealed record CallParticipantDto(
    Guid PersonId, DateTimeOffset JoinedAt, DateTimeOffset? LeftAt, bool AudioOn, bool VideoOn, bool SharingScreen)
{
    public static CallParticipantDto From(CallParticipant p) =>
        new(p.PersonId, p.JoinedAt, p.LeftAt, p.AudioOn, p.VideoOn, p.SharingScreen);
}

/// <summary>A call as clients read it. The media room name is not part of it; a join token is the only way in.</summary>
public sealed record CallDto(
    Guid Id,
    Guid SpaceId,
    string Kind,
    string State,
    string? EndReason,
    Guid InitiatorId,
    DateTimeOffset StartedAt,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? EndedAt,
    string Region,
    long Version,
    IReadOnlyList<CallParticipantDto> Participants)
{
    public static CallDto From(Call call) => new(
        call.Id,
        call.SpaceId,
        call.Kind.ToWire(),
        call.State.ToWire(),
        call.EndReason?.ToWire(),
        call.InitiatorId,
        call.StartedAt,
        call.AnsweredAt,
        call.EndedAt,
        call.Region,
        call.Version,
        call.Participants.OrderBy(p => p.JoinedAt).ThenBy(p => p.PersonId).Select(CallParticipantDto.From).ToList());
}

/// <summary>A room join ticket. Short-lived, for one room and one person; the client must not log it.</summary>
public sealed record CallJoinTokenDto(string Url, string Token, DateTimeOffset ExpiresAt);

/// <summary>A call and, when the caller is in it, their join token (null for an ended call, a busy answer or a no-op).</summary>
public sealed record CallJoinDto(CallDto Call, CallJoinTokenDto? JoinToken);

public sealed record CallPageDto(IReadOnlyList<CallDto> Items, string? NextCursor);
