using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Conversations.DTOs;

/// <summary>A participant of a conversation: who, with which role, since when. Only active participants are listed.</summary>
public sealed record ConversationParticipantDto(Guid UserId, string Role, DateTimeOffset JoinedAt);

/// <summary>
/// The newest message of a conversation as the viewer may read it. Text is null for a tombstone; it is cut to 140 characters.
/// </summary>
public sealed record ConversationPreviewDto(long Sequence, Guid AuthorId, string Kind, string? Text, DateTimeOffset CreatedAt);

/// <summary>
/// A direct or group conversation as one viewer sees it (UC-CHT-E01 to E05). Name is null for a direct conversation and for a
/// group without a name. IsHidden, NotificationLevel and UnreadCount are the viewer's own. LastActivityAt is the newest message's
/// time, or the time the conversation was made when no message exists.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    string Kind,
    string? Name,
    IReadOnlyList<ConversationParticipantDto> Participants,
    bool IsHidden,
    string NotificationLevel,
    int UnreadCount,
    ConversationPreviewDto? LastMessage,
    DateTimeOffset LastActivityAt,
    long Version,
    DateTimeOffset CreatedAt);

/// <summary>
/// The result of a start. Created is false when the direct conversation of the pair already existed (200). Skipped lists the
/// people of a group who were not added because they are not active in the Workspace or are not visible to a Guest caller.
/// </summary>
public sealed record StartConversationResultDto(ConversationDto Conversation, bool Created, IReadOnlyList<Guid> Skipped);

/// <summary>The people a participant add reached. Skipped people are not active members of the Workspace.</summary>
public sealed record AddConversationParticipantsResultDto(IReadOnlyList<Guid> Added, IReadOnlyList<Guid> Skipped);

/// <summary>The viewer's hide state of a conversation after a visibility change (UC-CHT-E07).</summary>
public sealed record ConversationVisibilityDto(Guid ConversationId, bool Hidden);

/// <summary>Builds the viewer's read model of conversations. Participants and memberships come from the loaded spaces.</summary>
public static class ConversationDtoBuilder
{
    public const int PreviewMaxLength = 140;

    public static ConversationDto From(
        Space space,
        Membership? viewerMembership,
        int unreadCount,
        ConversationPreviewDto? lastMessage)
    {
        var participants = space.Memberships
            .Where(m => m.IsActive)
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.UserId)
            .Select(m => new ConversationParticipantDto(m.UserId, m.Role.ToWire(), m.JoinedAt))
            .ToList();

        var viewerActive = viewerMembership is { IsActive: true };
        var lastActivity = lastMessage?.CreatedAt ?? space.CreatedAt;

        return new ConversationDto(
            space.Id,
            space.Kind.ToWire(),
            string.IsNullOrEmpty(space.Name) ? null : space.Name,
            participants,
            viewerActive && viewerMembership!.IsHidden,
            viewerActive ? viewerMembership!.NotificationLevel.ToWire() : NotificationLevel.All.ToWire(),
            unreadCount,
            lastMessage,
            lastActivity,
            space.Version,
            space.CreatedAt);
    }

    /// <summary>The preview of a message: the text cut to the preview length, or null for a tombstone.</summary>
    public static ConversationPreviewDto Preview(long sequence, Guid authorId, MessageKind kind, string? content, bool deleted, DateTimeOffset createdAt)
    {
        string? text = null;
        if (!deleted && content is not null)
        {
            var trimmed = content.Trim();
            text = trimmed.Length <= PreviewMaxLength ? trimmed : trimmed[..PreviewMaxLength];
        }
        return new ConversationPreviewDto(sequence, authorId, kind.ToWire(), text, createdAt);
    }
}
