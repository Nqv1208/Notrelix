using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Infrastructure.Chat;

namespace Notrelix.Infrastructure.Tests.Chat;

/// <summary>
/// The LiveKit adapter without a server: the access tokens it mints (claims, grants, lifetime), the webhook signature check (a valid
/// request passes; a tampered body, another key, an expired token, a missing or unsigned header are all refused) and how it speaks to the
/// server API (requests, an absent room or person, an unreachable server).
/// </summary>
public sealed class LiveKitChatCallMediaGatewayTests
{
    private const string Key = "unit-test-key";
    private const string Secret = "unit-test-secret-that-is-at-least-32-characters-long";
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    private sealed class ClockAt(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, string, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<(string Path, string Body, string? Authorization)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request.RequestUri!.AbsolutePath, body, request.Headers.Authorization?.Parameter));
            return answer(request, body);
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static LiveKitChatCallMediaGateway Gateway(
        HttpMessageHandler? handler = null, string key = Key, string secret = Secret, DateTimeOffset? now = null) =>
        new(
            new Factory(handler ?? new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") })),
            Microsoft.Extensions.Options.Options.Create(new ChatCallMediaOptions
            {
                Provider = ChatCallMediaProvider.LiveKit,
                ApiUrl = "http://livekit.test:7880",
                ApiKey = key,
                ApiSecret = secret,
            }),
            Microsoft.Extensions.Options.Options.Create(new ChatCallOptions { SfuUrl = "wss://media.test" }),
            new ClockAt(now ?? Now),
            NullLogger<LiveKitChatCallMediaGateway>.Instance);

    private static string SignWebhook(string body, string key = Key, string secret = Secret, DateTimeOffset? issued = null, TimeSpan? life = null, string? digest = null)
    {
        var at = issued ?? Now;
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: key,
            claims: [new System.Security.Claims.Claim("sha256", digest ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body))))],
            notBefore: at.UtcDateTime,
            expires: (at + (life ?? TimeSpan.FromMinutes(5))).UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static readonly Guid Person = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static string LeftBody(string id = "EV_1") =>
        $$"""{"event":"participant_left","id":"{{id}}","room":{"name":"room-abc","sid":"RM_1"},"participant":{"identity":"{{Person}}"},"createdAt":1}""";

    // ---- token minting -------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task AJoinTokenIsSignedForOnePersonAndOneRoomWithTheConfiguredLifetime()
    {
        var issued = await Gateway().IssueTokenAsync("room-abc", Person, canPublishVideo: false, TimeSpan.FromSeconds(60), default);

        issued.Url.Should().Be("wss://media.test");
        issued.ExpiresAt.Should().Be(Now.AddSeconds(60));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        jwt.Issuer.Should().Be(Key);
        jwt.Subject.Should().Be(Person.ToString());
        jwt.ValidTo.Should().Be(Now.AddSeconds(60).UtcDateTime);
        jwt.ValidFrom.Should().Be(Now.UtcDateTime);

        new JwtSecurityTokenHandler().ValidateToken(issued.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Key,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
        }, out _);
    }

    [Fact]
    public async Task AVoiceTokenCannotPublishACameraAndAVideoTokenCan()
    {
        static JsonElement Grant(string token) =>
            JsonDocument.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Payload["video"].ToString()!).RootElement.Clone();

        var voice = Grant((await Gateway().IssueTokenAsync("room-abc", Person, false, TimeSpan.FromSeconds(60), default)).Token);
        var video = Grant((await Gateway().IssueTokenAsync("room-abc", Person, true, TimeSpan.FromSeconds(60), default)).Token);

        voice.GetProperty("room").GetString().Should().Be("room-abc");
        voice.GetProperty("roomJoin").GetBoolean().Should().BeTrue();
        voice.GetProperty("canSubscribe").GetBoolean().Should().BeTrue();
        voice.GetProperty("canPublish").GetBoolean().Should().BeTrue();
        voice.GetProperty("canPublishSources").EnumerateArray().Select(e => e.GetString()).Should().Equal("microphone");
        voice.TryGetProperty("roomAdmin", out _).Should().BeFalse("a person's token never carries an admin grant");
        voice.TryGetProperty("roomCreate", out _).Should().BeFalse();
        video.GetProperty("canPublishSources").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo("microphone", "camera", "screen_share", "screen_share_audio");
    }

    // ---- webhook signature -------------------------------------------------------------------------------------------------------

    [Fact]
    public void ASignedWebhookIsAcceptedAndParsed()
    {
        var body = LeftBody();

        var verified = Gateway().VerifyWebhook(body, SignWebhook(body));

        verified.Should().Be(new CallMediaWebhookEvent("EV_1", "participant_left", "room-abc", Person));
    }

    [Fact]
    public void TheTimeTheParticipantWasSeatedIsReadFromTheSignedBody()
    {
        var withMs = $$$"""{"event":"participant_left","id":"EV_2","room":{"name":"room-abc"},"participant":{"identity":"{{{Person}}}","joinedAt":100,"joinedAtMs":1763370000123}}""";
        var withSeconds = $$$"""{"event":"participant_left","id":"EV_3","room":{"name":"room-abc"},"participant":{"identity":"{{{Person}}}","joinedAt":1763370000}}""";

        Gateway().VerifyWebhook(withMs, SignWebhook(withMs))!.ParticipantJoinedAt
            .Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1763370000123));
        Gateway().VerifyWebhook(withSeconds, SignWebhook(withSeconds))!.ParticipantJoinedAt
            .Should().Be(DateTimeOffset.FromUnixTimeSeconds(1763370000));
    }

    [Fact]
    public void TheBearerPrefixIsAcceptedBecauseSomeProxiesAddIt()
    {
        var body = LeftBody();
        Gateway().VerifyWebhook(body, "Bearer " + SignWebhook(body)).Should().NotBeNull();
    }

    [Fact]
    public void ATamperedBodyIsRefused()
    {
        var signedFor = LeftBody("EV_1");
        Gateway().VerifyWebhook(LeftBody("EV_2"), SignWebhook(signedFor)).Should().BeNull();
    }

    [Fact]
    public void AnotherKeyOrAnotherSecretIsRefused()
    {
        var body = LeftBody();
        Gateway().VerifyWebhook(body, SignWebhook(body, secret: "another-secret-that-is-at-least-32-characters!!")).Should().BeNull("wrong secret");
        Gateway().VerifyWebhook(body, SignWebhook(body, key: "someone-elses-key")).Should().BeNull("wrong issuer");
    }

    [Fact]
    public void AnExpiredTokenIsRefusedAndAFreshOneAtTheEdgeIsNot()
    {
        var body = LeftBody();
        var signed = SignWebhook(body, issued: Now.AddMinutes(-10), life: TimeSpan.FromMinutes(5));

        Gateway().VerifyWebhook(body, signed).Should().BeNull();
        Gateway(now: Now.AddMinutes(-7)).VerifyWebhook(body, signed).Should().NotBeNull("the same token is valid while its lifetime runs");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    public void AMissingOrUnsignedHeaderIsRefused(string? header) =>
        Gateway().VerifyWebhook(LeftBody(), header).Should().BeNull();

    [Fact]
    public void AnUnsignedTokenWithTheRightClaimsIsRefused()
    {
        var body = LeftBody();
        var unsigned = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Key,
            claims: [new System.Security.Claims.Claim("sha256", Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body))))],
            expires: Now.AddMinutes(5).UtcDateTime));

        Gateway().VerifyWebhook(body, unsigned).Should().BeNull();
    }

    [Fact]
    public void ATokenWithoutTheBodyDigestIsRefused()
    {
        var body = LeftBody();
        Gateway().VerifyWebhook(body, SignWebhook(body, digest: "")).Should().BeNull();
    }

    // ---- server API --------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ListParticipantsReadsTheIdentitiesAndAnAbsentRoomIsEmpty()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"participants":[{"identity":"{{Person}}"},{"identity":"not-a-guid"}]}"""),
        });
        (await Gateway(handler).ListParticipantsAsync("room-abc", default)).Should().Equal(Person);
        handler.Calls.Should().ContainSingle().Which.Path.Should().Be("/twirp/livekit.RoomService/ListParticipants");
        handler.Calls[0].Body.Should().Contain("\"room\":\"room-abc\"");
        new JwtSecurityTokenHandler().ReadJwtToken(handler.Calls[0].Authorization).Payload["video"].ToString().Should().Contain("roomAdmin");

        var absent = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        (await Gateway(absent).ListParticipantsAsync("room-gone", default)).Should().BeEmpty();
    }

    [Fact]
    public async Task RemovingAnAbsentPersonOrRoomIsHandled()
    {
        var absent = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));

        await Gateway(absent).RemoveParticipantAsync("room-abc", Person, default);
        await Gateway(absent).EndRoomAsync("room-abc", default);

        absent.Calls.Select(c => c.Path).Should().Equal(
            "/twirp/livekit.RoomService/RemoveParticipant", "/twirp/livekit.RoomService/DeleteRoom");
    }

    [Fact]
    public async Task ARevokedRightToPublishIsSentWithNoSourcesAndAGrantedOneWithTheCallKindSources()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var gateway = Gateway(handler);

        await gateway.UpdateParticipantPermissionAsync("room-abc", Person, canPublish: false, canPublishVideo: false, default);
        await gateway.UpdateParticipantPermissionAsync("room-abc", Person, canPublish: true, canPublishVideo: false, default);

        var muted = JsonDocument.Parse(handler.Calls[0].Body).RootElement.GetProperty("permission");
        muted.GetProperty("canPublish").GetBoolean().Should().BeFalse();
        muted.GetProperty("canPublishData").GetBoolean().Should().BeFalse();
        muted.GetProperty("canSubscribe").GetBoolean().Should().BeTrue("a muted person still hears the call");
        muted.GetProperty("canPublishSources").GetArrayLength().Should().Be(0);
        var voice = JsonDocument.Parse(handler.Calls[1].Body).RootElement.GetProperty("permission");
        voice.GetProperty("canPublish").GetBoolean().Should().BeTrue();
        voice.GetProperty("canPublishSources").EnumerateArray().Select(e => e.GetString()).Should().Equal("microphone");
    }

    [Fact]
    public async Task AFailingOrUnreachableServerIsTheMediaUnavailableError()
    {
        var failing = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var down = new StubHandler((_, _) => throw new HttpRequestException("connection refused"));

        await FluentActions.Awaiting(() => Gateway(failing).ListParticipantsAsync("room-abc", default)).Should().ThrowAsync<CallMediaUnavailableException>();
        await FluentActions.Awaiting(() => Gateway(down).RemoveParticipantAsync("room-abc", Person, default)).Should().ThrowAsync<CallMediaUnavailableException>();
    }

    // ---- options ------------------------------------------------------------------------------------------------------------------

    private sealed class Env(string name) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public void TheLiveKitOptionsAreValidatedAndTheDevKeyPairIsRefusedOutsideDevelopment()
    {
        var good = new ChatCallMediaOptions { Provider = ChatCallMediaProvider.LiveKit, ApiUrl = "http://livekit:7880", ApiKey = "k", ApiSecret = Secret };
        new ChatCallMediaOptionsValidator(new Env("Production")).Validate(null, good).Succeeded.Should().BeTrue();
        new ChatCallMediaOptionsValidator(new Env("Production")).Validate(null, new ChatCallMediaOptions()).Succeeded.Should().BeTrue("InMemory needs nothing");

        var bad = new ChatCallMediaOptions { Provider = ChatCallMediaProvider.LiveKit, ApiUrl = "livekit", ApiKey = "", ApiSecret = "short" };
        new ChatCallMediaOptionsValidator(new Env("Production")).Validate(null, bad).Failures.Should().HaveCount(3);

        var dev = new ChatCallMediaOptions
        {
            Provider = ChatCallMediaProvider.LiveKit,
            ApiUrl = "http://livekit:7880",
            ApiKey = ChatCallMediaOptions.DevApiKey,
            ApiSecret = ChatCallMediaOptions.DevApiSecret,
        };
        new ChatCallMediaOptionsValidator(new Env("Development")).Validate(null, dev).Succeeded.Should().BeTrue();
        new ChatCallMediaOptionsValidator(new Env("Staging")).Validate(null, dev).Succeeded.Should().BeFalse();
        new ChatCallMediaOptionsValidator(new Env("Production")).Validate(null, dev).Succeeded.Should().BeFalse();
    }
}
