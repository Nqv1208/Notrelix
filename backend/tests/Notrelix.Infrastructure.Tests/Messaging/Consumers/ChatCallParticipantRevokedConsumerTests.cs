using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Infrastructure.Messaging.Consumers.Chat;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// The media kick of a revoked person is at-least-once: when the media plane fails the first time, the consumer does not swallow the
/// failure and the endpoint's retry policy delivers the message again until the gateway accepts it.
/// </summary>
public sealed class ChatCallParticipantRevokedConsumerTests
{
    private sealed class FlakyGateway : IChatCallMediaGateway
    {
        public int Attempts;
        public List<(string RoomKey, Guid PersonId)> Removed { get; } = [];

        public Task RemoveParticipantAsync(string roomKey, Guid personId, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref Attempts) == 1)
                throw new CallMediaUnavailableException("The media plane is down.");
            lock (Removed) Removed.Add((roomKey, personId));
            return Task.CompletedTask;
        }

        public Task<CallRoom> CreateRoomAsync(string roomKey, string region, int maxParticipants, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CallJoinToken> IssueTokenAsync(string roomKey, Guid personId, bool canPublishVideo, TimeSpan ttl, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EndRoomAsync(string roomKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListParticipantsAsync(string roomKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListRoomsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public CallMediaWebhookEvent? VerifyWebhook(string rawBody, string? authorization) => null;
    }

    [Fact]
    public async Task AFailedKickIsRetriedUntilTheMediaPlaneRecordsIt()
    {
        var gateway = new FlakyGateway();
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IChatCallMediaGateway>(gateway)
            .AddMassTransitTestHarness(x => x.AddConsumer<ChatCallParticipantRevokedConsumer, ChatCallParticipantRevokedConsumerDefinition>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var person = Guid.NewGuid();
        await harness.Bus.Publish(new ChatCallParticipantRevokedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), person, "room-1", Guid.NewGuid()));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && gateway.Removed.Count == 0)
            await Task.Delay(100);

        gateway.Attempts.Should().BeGreaterThanOrEqualTo(2, "the first attempt failed and was retried");
        gateway.Removed.Should().ContainSingle(r => r.PersonId == person && r.RoomKey == "room-1");
    }
}
