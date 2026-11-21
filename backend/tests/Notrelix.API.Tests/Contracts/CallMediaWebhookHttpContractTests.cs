using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Commands.HandleCallMediaWebhook;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// HTTP contract of the media server webhook, with CSRF enforcement on: the route is exempt from the browser CSRF gate by its own
/// declaration (never a blanket skip), so an unsigned or wrongly signed request is the endpoint's own 401 and never a CSRF 403, and
/// nothing reaches the call processor. A verified request is applied once, whichever of the two JSON media types it arrives in.
/// </summary>
public class CallMediaWebhookHttpContractTests
{
    private const string Route = "/api/v1/chat/calls/media-webhook";
    private const string Body = """{"event":"room_finished","id":"EV_9","room":{"name":"room-x"}}""";

    private sealed class RecordingProcessor : IChatCallMediaWebhookProcessor
    {
        public List<CallMediaWebhookEvent> Processed { get; } = [];

        public Task ProcessAsync(CallMediaWebhookEvent mediaEvent, string rawBody, CancellationToken cancellationToken)
        {
            lock (Processed) Processed.Add(mediaEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class VerifyingGateway(string validAuthorization) : IChatCallMediaGateway
    {
        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) =>
            authorization == validAuthorization && rawBody == Body ? new CallMediaWebhookEvent("EV_9", "room_finished", "room-x", null) : null;

        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateParticipantPermissionAsync(string roomKey, Guid personId, bool canPublish, bool canPublishVideo, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Factory(RecordingProcessor processor) : CsrfEnabledApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IChatCallMediaGateway>();
                services.AddSingleton<IChatCallMediaGateway>(new VerifyingGateway("good-signature"));
                services.RemoveAll<IChatCallMediaWebhookProcessor>();
                services.AddSingleton<IChatCallMediaWebhookProcessor>(processor);
            });
        }
    }

    private static HttpRequestMessage Post(string? authorization, string mediaType = "application/webhook+json", string body = Body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Route) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
        if (authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        return request;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("forged-signature")]
    public async Task AnUnsignedOrWronglySignedRequest_Is401NeverCsrfForbidden_AndNothingIsProcessed(string? authorization)
    {
        var processor = new RecordingProcessor();
        await using var factory = new Factory(processor);

        var response = await factory.CreateClient().SendAsync(Post(authorization));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        processor.Processed.Should().BeEmpty();
    }

    [Fact]
    public async Task ATamperedBodyUnderAValidSignatureIs401()
    {
        var processor = new RecordingProcessor();
        await using var factory = new Factory(processor);

        var response = await factory.CreateClient().SendAsync(Post("good-signature", body: Body.Replace("room-x", "room-y")));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        processor.Processed.Should().BeEmpty();
    }

    [Theory]
    [InlineData("application/webhook+json")]
    [InlineData("application/json")]
    public async Task AVerifiedRequestWithoutCsrfMaterialIsProcessedOnce(string mediaType)
    {
        var processor = new RecordingProcessor();
        await using var factory = new Factory(processor);

        var response = await factory.CreateClient().SendAsync(Post("good-signature", mediaType));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        processor.Processed.Should().ContainSingle().Which.EventId.Should().Be("EV_9");
    }

    [Fact]
    public async Task AnotherMediaTypeIs415()
    {
        await using var factory = new Factory(new RecordingProcessor());

        var response = await factory.CreateClient().SendAsync(Post("good-signature", "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }
}
