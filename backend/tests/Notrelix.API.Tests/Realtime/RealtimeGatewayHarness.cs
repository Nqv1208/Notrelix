using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// Runs a real <see cref="RealtimeConnection"/> against an in-memory WebSocket pair
/// (real framing, no HTTP) with fake authorization, credential and notification sources.
/// </summary>
internal sealed class RealtimeGatewayHarness : IAsyncDisposable
{
    public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AccountId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly WebSocket _client;
    private readonly Task _run;

    public RealtimeGatewayHarness(
        RealtimeGatewayOptions? options = null,
        FakeAuthorizer? authorizer = null,
        FakeCredentials? credentials = null,
        DateTimeOffset? credentialExpiresAt = null)
    {
        Options = options ?? FastOptions();
        Authorizer = authorizer ?? new FakeAuthorizer();
        Credentials = credentials ?? new FakeCredentials();
        Source = new Notrelix.Infrastructure.Realtime.InMemoryRealtimeNotificationSource(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Notrelix.Infrastructure.Realtime.InMemoryRealtimeNotificationSource>.Instance);

        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        var serverStream = new DuplexStream(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream());
        var clientStream = new DuplexStream(serverToClient.Reader.AsStream(), clientToServer.Writer.AsStream());
        var server = WebSocket.CreateFromStream(serverStream, new WebSocketCreationOptions { IsServer = true });
        _client = WebSocket.CreateFromStream(clientStream, new WebSocketCreationOptions { IsServer = false });

        var principal = new RealtimePrincipal(
            UserId, "user@example.com", "User", "jti-1", null, DateTimeOffset.UtcNow, credentialExpiresAt, Guid.NewGuid());
        Connection = new RealtimeConnection(
            server, principal, Options, Authorizer, Credentials, Source,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        _run = Task.Run(() => Connection.RunAsync(CancellationToken.None));
    }

    public RealtimeGatewayOptions Options { get; }
    public FakeAuthorizer Authorizer { get; }
    public FakeCredentials Credentials { get; }
    public Notrelix.Infrastructure.Realtime.InMemoryRealtimeNotificationSource Source { get; }
    public RealtimeConnection Connection { get; }
    public WebSocket Client => _client;
    public Task Completed => _run;

    public static RealtimeGatewayOptions FastOptions() => new()
    {
        PingInterval = TimeSpan.FromMilliseconds(200),
        IdleTimeout = TimeSpan.FromSeconds(5),
        ReauthorizationInterval = TimeSpan.FromMilliseconds(200),
        SendTimeout = TimeSpan.FromSeconds(2),
    };

    public async Task SendAsync(object message)
        => await _client.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)), WebSocketMessageType.Text, true, CancellationToken.None);

    public async Task SendRawAsync(string text)
        => await _client.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    public async Task<JsonElement> ReceiveAsync(string? type = null, int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        while (true)
        {
            var (message, closed) = await ReceiveFrameAsync(cts.Token);
            if (closed)
            {
                throw new InvalidOperationException($"socket closed ({_client.CloseStatus}) while waiting for {type}");
            }

            if (type is null || message.GetProperty("type").GetString() == type)
            {
                return message;
            }
        }
    }

    /// <summary>Reads frames until the server closes; returns the close status.</summary>
    public async Task<(WebSocketCloseStatus? Status, string? Reason)> ReceiveUntilCloseAsync(int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        while (true)
        {
            var (_, closed) = await ReceiveFrameAsync(cts.Token);
            if (closed)
            {
                return (_client.CloseStatus, _client.CloseStatusDescription);
            }
        }
    }

    private async Task<(JsonElement Message, bool Closed)> ReceiveFrameAsync(CancellationToken token)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();
        ValueWebSocketReceiveResult result;
        do
        {
            result = await _client.ReceiveAsync(buffer.AsMemory(), token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                // Like a browser, acknowledge the server's close frame.
                if (_client.State == WebSocketState.CloseReceived)
                {
                    await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ack", CancellationToken.None);
                }

                return (default, true);
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        using var document = JsonDocument.Parse(stream.ToArray());
        return (document.RootElement.Clone(), false);
    }

    public async Task<JsonElement> SubscribeAsync(string subscriptionId, string kind, Guid id)
    {
        await SendAsync(new { type = "subscribe", subscriptionId, scope = new { kind, id } });
        return await ReceiveAsync();
    }

    public static RealtimeNotification Notification(
        RealtimeScope scope, long version, Guid? workspaceId = null, string? streamKey = null) => new(
        EventId: Guid.NewGuid().ToString("N"),
        EventType: "board.board",
        ChangeKind: "CreateBoardItem",
        StreamKey: streamKey ?? scope.StreamKey,
        StreamVersion: version,
        ResourceKind: "Board",
        ResourceId: scope.Id,
        WorkspaceId: workspaceId ?? WorkspaceId,
        CorrelationId: Guid.NewGuid().ToString("N"),
        OccurredAt: DateTimeOffset.UtcNow,
        ResourceVersion: 7,
        SchemaVersion: 1,
        PayloadContract: "Test.Dto.v1");

    public async ValueTask DisposeAsync()
    {
        Connection.RequestClose(WebSocketCloseStatus.NormalClosure, "test_done");
        try
        {
            // Acknowledge the server's close frame like a browser would.
            _ = ReceiveUntilCloseAsync().ContinueWith(_ => { }, TaskScheduler.Default);
            await _run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
        }

        _client.Dispose();
    }

    private sealed class DuplexStream(Stream read, Stream write) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => write.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => write.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => read.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => read.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => write.Write(buffer, offset, count);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => write.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                write.Dispose();
                read.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

internal sealed class FakeAuthorizer : IRealtimeSubscriptionAuthorizer
{
    private readonly Dictionary<RealtimeScope, RealtimeAuthorizationResult> _results = [];
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public RealtimeAuthorizationResult Default { get; set; } = RealtimeAuthorizationResult.Granted(
        new RealtimeSubscriptionGrant(RealtimeGatewayHarness.AccountId, RealtimeGatewayHarness.WorkspaceId));

    public void Set(RealtimeScope scope, RealtimeAuthorizationResult result)
    {
        lock (_results)
        {
            _results[scope] = result;
        }
    }

    public Task<RealtimeAuthorizationResult> AuthorizeAsync(
        RealtimePrincipal principal, RealtimeScope scope, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        lock (_results)
        {
            return Task.FromResult(_results.TryGetValue(scope, out var result) ? result : Default);
        }
    }
}

internal sealed class FakeCredentials : IRealtimeCredentialValidator
{
    public volatile bool Valid = true;

    public Task<bool> IsStillValidAsync(RealtimePrincipal principal, CancellationToken cancellationToken)
        => Task.FromResult(Valid);
}
