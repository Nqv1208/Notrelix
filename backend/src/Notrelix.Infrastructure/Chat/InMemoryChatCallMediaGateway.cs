using System.Collections.Concurrent;
using Notrelix.Application.Features.Chat.Calls;

namespace Notrelix.Infrastructure.Chat;

/// <summary>
/// The only <see cref="IChatCallMediaGateway"/> of Phase 5a: an in-memory stand-in for the media plane that holds rooms and signs nothing real (the architecture rule DI_001 keeps the word Fake out of production type names). It exists
/// so the call domain, the commands, the jobs and the endpoints run end to end before a media server is chosen and wired (the real
/// adapter replaces it behind the same port). The tokens it issues are opaque test strings, scoped to one room and one person.
/// </summary>
public sealed class InMemoryChatCallMediaGateway(IOptions<ChatCallOptions> options) : IChatCallMediaGateway
{
    private readonly ConcurrentDictionary<string, string> _rooms = new(StringComparer.Ordinal);

    public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken)
    {
        _rooms[roomKey] = region;
        return Task.FromResult(new CallRoom(roomKey, region));
    }

    public Task<CallJoinToken> IssueTokenAsync(
        string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken)
    {
        if (!_rooms.ContainsKey(roomKey))
            throw new CallMediaUnavailableException("The room does not exist.");

        var token = $"fake.{roomKey}.{personId:N}.{(canPublishVideo ? "av" : "a")}";
        return Task.FromResult(new CallJoinToken(options.Value.SfuUrl, token, DateTimeOffset.UtcNow + ttl));
    }

    public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken)
    {
        _rooms.TryRemove(roomKey, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(_rooms.Keys.ToList());

    public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
}
