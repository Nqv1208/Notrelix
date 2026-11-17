using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Notrelix.Application.Features.Chat.Calls;

namespace Notrelix.Infrastructure.Chat;

/// <summary>
/// The LiveKit adapter behind <see cref="IChatCallMediaGateway"/> (SYS-ADR-002 decision 7). Self-hosted LiveKit and LiveKit Cloud are the
/// same adapter with a different address and key pair. It signs LiveKit access tokens with the JWT library the platform already uses and
/// calls the server's RoomService over Twirp with plain JSON, so it adds no dependency (the vendor SDK would bring protobuf for a few
/// calls). Every transport failure becomes <see cref="CallMediaUnavailableException"/>, which the handlers answer as
/// <c>chat.calls.unavailable</c>. A token is never logged.
/// </summary>
public sealed class LiveKitChatCallMediaGateway(
    IHttpClientFactory httpClients,
    IOptions<ChatCallMediaOptions> media,
    IOptions<ChatCallOptions> calls,
    TimeProvider time,
    ILogger<LiveKitChatCallMediaGateway> logger) : IChatCallMediaGateway
{
    public const string HttpClientName = "livekit-room-service";

    private const string Service = "twirp/livekit.RoomService/";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly JsonWebTokenHandler Tokens = new();

    private ChatCallMediaOptions Options => media.Value;

    public async Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken)
    {
        await CallAsync("CreateRoom",
            new { name = roomKey, emptyTimeout = Options.RoomEmptyTimeoutSeconds, maxParticipants },
            new Grant { RoomCreate = true }, cancellationToken);
        return new CallRoom(roomKey, region);
    }

    public Task<CallJoinToken> IssueTokenAsync(
        string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var expires = now + ttl;
        var token = Sign(personId.ToString(), new Grant
        {
            Room = roomKey,
            RoomJoin = true,
            CanSubscribe = true,
            CanPublish = true,
            CanPublishData = true,
            CanPublishSources = Sources(canPublishVideo),
        }, now, expires);
        return Task.FromResult(new CallJoinToken(calls.Value.SfuUrl, token, expires));
    }

    public async Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) =>
        await CallAsync("DeleteRoom", new { room = roomKey }, new Grant { RoomCreate = true }, cancellationToken, absentIsFine: true);

    public async Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken) =>
        await CallAsync("RemoveParticipant", new { room = roomKey, identity = personId.ToString() },
            new Grant { RoomAdmin = true, Room = roomKey }, cancellationToken, absentIsFine: true);

    public async Task UpdateParticipantPermissionAsync(
        string roomKey, Guid personId, bool canPublish, bool canPublishVideo, CancellationToken cancellationToken) =>
        await CallAsync("UpdateParticipant",
            new
            {
                room = roomKey,
                identity = personId.ToString(),
                permission = new
                {
                    canSubscribe = true,
                    canPublish,
                    canPublishData = canPublish,
                    canPublishSources = canPublish ? Sources(canPublishVideo) : [],
                },
            },
            new Grant { RoomAdmin = true, Room = roomKey }, cancellationToken, absentIsFine: true);

    public async Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken)
    {
        var answer = await CallAsync("ListParticipants", new { room = roomKey },
            new Grant { RoomAdmin = true, Room = roomKey }, cancellationToken, absentIsFine: true);
        if (answer is not { } body || !body.TryGetProperty("participants", out var list)) return [];

        return list.EnumerateArray()
            .Select(p => p.TryGetProperty("identity", out var id) && Guid.TryParse(id.GetString(), out var person) ? person : (Guid?)null)
            .OfType<Guid>()
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        var answer = await CallAsync("ListRooms", new { }, new Grant { RoomList = true }, cancellationToken);
        if (answer is not { } body || !body.TryGetProperty("rooms", out var list)) return [];

        return list.EnumerateArray().Select(r => r.GetProperty("name").GetString()).OfType<string>().ToList();
    }

    /// <summary>
    /// LiveKit signs every webhook with a JWT whose issuer is the API key and whose <c>sha256</c> claim is the base64 digest of the exact
    /// body. The signature, the key, the lifetime and the body digest are all checked before the body is parsed; anything else is null.
    /// </summary>
    public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) || string.IsNullOrEmpty(rawBody)) return null;

        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : authorization.Trim();
        var checkedToken = Tokens.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Options.ApiKey,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            LifetimeValidator = (notBefore, expires, _, _) =>
                expires is { } end && time.GetUtcNow().UtcDateTime < end.ToUniversalTime().AddSeconds(30)
                && (notBefore is not { } start || time.GetUtcNow().UtcDateTime >= start.ToUniversalTime().AddSeconds(-30)),
        }).GetAwaiter().GetResult();
        if (!checkedToken.IsValid || checkedToken.SecurityToken is not JsonWebToken jwt) return null;
        if (!jwt.TryGetPayloadValue<string>("sha256", out var claimed) || string.IsNullOrEmpty(claimed)) return null;

        var actual = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(claimed), Encoding.ASCII.GetBytes(actual))) return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var kind = root.TryGetProperty("event", out var e) ? e.GetString() : null;
            var id = root.TryGetProperty("id", out var i) ? i.GetString() : null;
            var room = root.TryGetProperty("room", out var r) && r.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(room)) return null;

            Guid? person = root.TryGetProperty("participant", out var p) && p.TryGetProperty("identity", out var who)
                && Guid.TryParse(who.GetString(), out var parsed) ? parsed : null;
            // LiveKit stamps the participant with the time its session was seated (joinedAtMs, else joinedAt in seconds).
            DateTimeOffset? seated = null;
            if (root.TryGetProperty("participant", out var seat))
            {
                if (seat.TryGetProperty("joinedAtMs", out var ms) && ms.TryGetInt64(out var millis) && millis > 0)
                    seated = DateTimeOffset.FromUnixTimeMilliseconds(millis);
                else if (seat.TryGetProperty("joinedAt", out var sec) && sec.TryGetInt64(out var seconds) && seconds > 0)
                    seated = DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
            return new CallMediaWebhookEvent(id, kind, room, person, seated);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // A voice call carries no camera or screen; a video call may publish all four sources.
    private static string[] Sources(bool video) =>
        video ? ["microphone", "camera", "screen_share", "screen_share_audio"] : ["microphone"];

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Options.ApiSecret));

    private string Sign(string identity, Grant grant, DateTimeOffset now, DateTimeOffset expires) =>
        Tokens.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Options.ApiKey,
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object> { ["sub"] = identity, ["video"] = grant.ToClaim() },
        });

    /// <returns>The JSON answer, or null when the thing is absent and that is fine for the caller.</returns>
    private async Task<JsonElement?> CallAsync(
        string method, object body, Grant grant, CancellationToken cancellationToken, bool absentIsFine = false)
    {
        var now = time.GetUtcNow();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(Options.ApiUrl.TrimEnd('/') + "/"), Service + method))
        {
            Content = JsonContent.Create(body, options: Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Sign("notrelix-server", grant, now, now.AddMinutes(1)));

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Options.RequestTimeoutSeconds));
            using var response = await httpClients.CreateClient(HttpClientName).SendAsync(request, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound && absentIsFine) return null;
            if (!response.IsSuccessStatusCode)
                throw new CallMediaUnavailableException($"The media server answered {(int)response.StatusCode} to {method}.");

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token)).RootElement.Clone();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The media server call {Method} failed: {Reason}", method, ex.GetType().Name);
            throw new CallMediaUnavailableException($"The media server could not be reached for {method}.", ex);
        }
    }

    private sealed class Grant
    {
        public string? Room { get; init; }
        public bool RoomJoin { get; init; }
        public bool RoomCreate { get; init; }
        public bool RoomList { get; init; }
        public bool RoomAdmin { get; init; }
        public bool CanSubscribe { get; init; }
        public bool CanPublish { get; init; }
        public bool CanPublishData { get; init; }
        public string[]? CanPublishSources { get; init; }

        public Dictionary<string, object> ToClaim()
        {
            var claim = new Dictionary<string, object>();
            if (Room is not null) claim["room"] = Room;
            if (RoomJoin) claim["roomJoin"] = true;
            if (RoomCreate) claim["roomCreate"] = true;
            if (RoomList) claim["roomList"] = true;
            if (RoomAdmin) claim["roomAdmin"] = true;
            if (RoomJoin)
            {
                claim["canSubscribe"] = CanSubscribe;
                claim["canPublish"] = CanPublish;
                claim["canPublishData"] = CanPublishData;
                if (CanPublishSources is not null) claim["canPublishSources"] = CanPublishSources;
            }

            return claim;
        }
    }
}
