using System.Net.WebSockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Infrastructure.Chat;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// The LiveKit adapter against a real LiveKit server (the image the dev compose pins): a token the adapter signs is accepted by the
/// server, a fresh room lists nobody, a person who joined is listed and a kick removes them, and removing someone who is absent, or ending
/// a room that is gone, is handled. A wrong secret is the media-unavailable error. Needs Docker locally; run it under the heavy lock.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LiveKitContract")]
public sealed class LiveKitChatCallMediaGatewayContractTests : IAsyncLifetime
{
    /// <summary>Must equal the image of the <c>livekit</c> service in <c>docker-compose.dev.yml</c> (checked by a test below).</summary>
    public const string Image = "livekit/livekit-server:v1.13.0";

    private const string Key = "contract-key";
    private const string Secret = "contract-secret-that-is-at-least-32-characters-long";

    private IContainer? _server;
    private string _http = "";
    private string _ws = "";

    public async Task InitializeAsync()
    {
        _server = new ContainerBuilder()
            .WithImage(Image)
            .WithName($"notrelix-test-livekit-{Guid.NewGuid():N}")
            .WithEnvironment("LIVEKIT_KEYS", $"{Key}: {Secret}")
            .WithCommand("--node-ip", "127.0.0.1")
            .WithPortBinding(7880, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7880).ForPath("/")))
            .Build();
        await _server.StartAsync();
        var port = _server.GetMappedPublicPort(7880);
        _http = $"http://localhost:{port}";
        _ws = $"ws://localhost:{port}";
    }

    public async Task DisposeAsync()
    {
        if (_server is not null) await _server.DisposeAsync();
    }

    private LiveKitChatCallMediaGateway Gateway(string secret = Secret)
    {
        return new LiveKitChatCallMediaGateway(
            new PlainHttpClients(),
            Options.Create(new ChatCallMediaOptions
            {
                Provider = ChatCallMediaProvider.LiveKit,
                ApiUrl = _http,
                ApiKey = Key,
                ApiSecret = secret,
                RequestTimeoutSeconds = 10,
            }),
            Options.Create(new ChatCallOptions { SfuUrl = _ws }),
            TimeProvider.System,
            NullLogger<LiveKitChatCallMediaGateway>.Instance);
    }

    private sealed class PlainHttpClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static string NewRoom() => $"room-{Guid.NewGuid():N}";

    [Fact]
    public async Task AFreshRoomIsListedAndHoldsNobody_AndEndingItRemovesIt()
    {
        var gateway = Gateway();
        var room = NewRoom();

        (await gateway.CreateRoomAsync(room, "ap-southeast-1", 25, default)).RoomKey.Should().Be(room);

        (await gateway.ListRoomsAsync(default)).Should().Contain(room);
        (await gateway.ListParticipantsAsync(room, default)).Should().BeEmpty();

        await gateway.EndRoomAsync(room, default);
        (await gateway.ListRoomsAsync(default)).Should().NotContain(room);
        await gateway.EndRoomAsync(room, default); // a room that is already gone is handled
    }

    [Fact]
    public async Task RemovingAnAbsentPersonOrUpdatingOneAndListingAMissingRoomAreHandled()
    {
        var gateway = Gateway();
        var room = NewRoom();
        await gateway.CreateRoomAsync(room, "ap-southeast-1", 25, default);

        await gateway.RemoveParticipantAsync(room, Guid.NewGuid(), default);
        await gateway.UpdateParticipantPermissionAsync(room, Guid.NewGuid(), canPublish: false, canPublishVideo: false, default);
        await gateway.RemoveParticipantAsync(NewRoom(), Guid.NewGuid(), default);
        (await gateway.ListParticipantsAsync(NewRoom(), default)).Should().BeEmpty();
    }

    [Fact]
    public async Task ATokenTheAdapterSignsIsAcceptedByTheServer_AndAKickRemovesThePersonWhoJoined()
    {
        var gateway = Gateway();
        var room = NewRoom();
        var person = Guid.NewGuid();
        await gateway.CreateRoomAsync(room, "ap-southeast-1", 25, default);
        var token = await gateway.IssueTokenAsync(room, person, canPublishVideo: false, TimeSpan.FromSeconds(60), default);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(
            new Uri($"{_ws}/rtc?access_token={Uri.EscapeDataString(token.Token)}&auto_subscribe=1&sdk=csharp&protocol=15&version=1.0.0"),
            default);
        socket.State.Should().Be(WebSocketState.Open, "the server accepted the signed token");

        (await WaitForAsync(async () => (await gateway.ListParticipantsAsync(room, default)).Contains(person)))
            .Should().BeTrue("the server lists the person who joined");

        await gateway.UpdateParticipantPermissionAsync(room, person, canPublish: false, canPublishVideo: false, default);
        await gateway.RemoveParticipantAsync(room, person, default);
        (await WaitForAsync(async () => !(await gateway.ListParticipantsAsync(room, default)).Contains(person)))
            .Should().BeTrue("the kick removed them from the live room");
    }

    [Fact]
    public async Task AWrongSecretIsTheMediaUnavailableError_AndAnUnreachableServerIsToo()
    {
        await FluentActions.Awaiting(() => Gateway("a-different-secret-that-is-at-least-32-chars!").CreateRoomAsync(NewRoom(), "ap-southeast-1", 25, default))
            .Should().ThrowAsync<CallMediaUnavailableException>("the server refuses a token signed with another secret");

        var unreachable = Gateway();
        await _server!.StopAsync();
        await FluentActions.Awaiting(() => unreachable.ListRoomsAsync(default)).Should().ThrowAsync<CallMediaUnavailableException>();
    }

    [Fact]
    public void ThePinnedImageIsTheOneTheDevComposeStarts()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.dev.yml"))) directory = directory.Parent;

        directory.Should().NotBeNull("the repository root holds docker-compose.dev.yml");
        File.ReadAllText(Path.Combine(directory!.FullName, "docker-compose.dev.yml")).Should().Contain($"image: {Image}");
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 40; i++)
        {
            if (await condition()) return true;
            await Task.Delay(250);
        }

        return false;
    }
}
