using System.Net.WebSockets;
using System.Threading.Channels;

using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Realtime;

/// <summary>
/// One authenticated gateway connection: protocol loop, subscription lifecycle,
/// bounded outbound queue and periodic re-authorization. A connection owns no
/// durable state and there is no replay buffer; after any close the client
/// reconnects, resubscribes and refetches authoritative state over REST.
/// </summary>
public sealed class RealtimeConnection
{
    private readonly WebSocket _socket;
    private readonly RealtimePrincipal _principal;
    private readonly RealtimeGatewayOptions _options;
    private readonly IRealtimeSubscriptionAuthorizer _authorizer;
    private readonly IRealtimeCredentialValidator _credentials;
    private readonly IRealtimeNotificationSource _source;
    private readonly IRealtimeAccessSignalSource? _accessSource;
    private readonly Dictionary<Guid, IAsyncDisposable> _accessHandles = new();
    private readonly ILogger _logger;
    private readonly TimeProvider _clock;

    private readonly Channel<byte[]> _outbox;
    private readonly SemaphoreSlim _subscriptionGate = new(1, 1);
    private readonly Dictionary<string, Subscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationTokenSource _stopReceive = new();
    private readonly DateTimeOffset _startedAt;

    private int _closeRequested;
    private WebSocketCloseStatus _closeStatus = RealtimeProtocol.CloseCodes.Normal;
    private string _closeReason = "closed";
    private long _lastInboundTicks;
    private long _rateWindowStartTicks;
    private int _rateWindowCount;

    public RealtimeConnection(
        WebSocket socket,
        RealtimePrincipal principal,
        RealtimeGatewayOptions options,
        IRealtimeSubscriptionAuthorizer authorizer,
        IRealtimeCredentialValidator credentials,
        IRealtimeNotificationSource source,
        ILogger logger,
        TimeProvider? clock = null,
        IRealtimeAccessSignalSource? accessSource = null)
    {
        _accessSource = accessSource;
        _socket = socket;
        _principal = principal;
        _options = options;
        _authorizer = authorizer;
        _credentials = credentials;
        _source = source;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
        _startedAt = _clock.GetUtcNow();
        ConnectionId = Guid.NewGuid();
        _outbox = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(options.SendQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _lastInboundTicks = _clock.GetTimestamp();
        _rateWindowStartTicks = _lastInboundTicks;
    }

    public Guid ConnectionId { get; }

    public int SubscriptionCount
    {
        get
        {
            lock (_subscriptions)
            {
                return _subscriptions.Count;
            }
        }
    }

    /// <summary>Runs the connection until it closes. Never throws for transport errors.</summary>
    public async Task RunAsync(CancellationToken requestAborted)
    {
        using var sendLinked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, requestAborted);
        using var receiveLinked = CancellationTokenSource.CreateLinkedTokenSource(_stopReceive.Token, requestAborted);

        _logger.LogInformation(
            "Realtime connection {ConnectionId} opened correlation {CorrelationId}",
            ConnectionId, _principal.CorrelationId);

        Enqueue(RealtimeProtocol.Ready(ConnectionId, _options));

        var send = SendLoopAsync(sendLinked.Token);
        var receive = ReceiveLoopAsync(receiveLinked.Token);
        var maintain = MaintenanceLoopAsync(sendLinked.Token);

        try
        {
            await Task.WhenAny(send, receive, maintain).ConfigureAwait(false);
        }
        finally
        {
            if (requestAborted.IsCancellationRequested)
            {
                RequestClose(RealtimeProtocol.CloseCodes.GoingAway, "server_shutdown");
            }

            RequestClose(RealtimeProtocol.CloseCodes.Normal, "closed");
            _outbox.Writer.TryComplete();

            // Stop producing first, then close the socket while the receive loop is
            // still reading so the close handshake can complete; only then stop it.
            await _stop.CancelAsync().ConfigureAwait(false);
            await Task.WhenAll(Quiet(send), Quiet(maintain)).ConfigureAwait(false);
            await ReleaseSubscriptionsAsync().ConfigureAwait(false);
            await CloseSocketAsync().ConfigureAwait(false);
            await WaitForCloseAcknowledgementAsync(receive).ConfigureAwait(false);

            _logger.LogInformation(
                "Realtime connection {ConnectionId} closed code {CloseCode} reason {CloseReason} duration {DurationMs}ms",
                ConnectionId, (int)_closeStatus, _closeReason,
                (long)(_clock.GetUtcNow() - _startedAt).TotalMilliseconds);
        }
    }

    private async Task WaitForCloseAcknowledgementAsync(Task receive)
    {
        try
        {
            await receive.WaitAsync(_options.SendTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await _stopReceive.CancelAsync().ConfigureAwait(false);
            _socket.Abort();
            await Quiet(receive).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Receive failures are already translated into a close request.
        }
    }

    // ── Close handling ──────────────────────────────────────

    /// <summary>Records the first close request and stops all loops.</summary>
    public void RequestClose(WebSocketCloseStatus status, string reason)
    {
        if (Interlocked.Exchange(ref _closeRequested, 1) != 0)
        {
            return;
        }

        _closeStatus = status;
        _closeReason = reason;
        try
        {
            _stop.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Connection already torn down.
        }
    }

    private async Task CloseSocketAsync()
    {
        try
        {
            if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeout = new CancellationTokenSource(_options.SendTimeout);
                await _socket.CloseOutputAsync(_closeStatus, _closeReason, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            _socket.Abort();
        }
    }

    private static async Task Quiet(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Loop failures are already translated into a close request.
        }
    }

    // ── Send ────────────────────────────────────────────────

    private bool Enqueue(byte[] frame)
    {
        if (_outbox.Writer.TryWrite(frame))
        {
            return true;
        }

        // The consumer cannot keep up and there is no replay buffer: close so the
        // client reconnects and refetches instead of silently losing notifications.
        RequestClose(RealtimeProtocol.CloseCodes.SlowConsumer, "slow_consumer");
        return false;
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var frame in _outbox.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(_options.SendTimeout);
                await _socket.SendAsync(frame, WebSocketMessageType.Text, true, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            RequestClose(RealtimeProtocol.CloseCodes.SlowConsumer, "send_failed");
        }
    }

    // ── Receive ─────────────────────────────────────────────

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[Math.Min(_options.MaxInboundMessageBytes, 4096)];
        using var message = new MemoryStream();
        try
        {
            while (!token.IsCancellationRequested && _socket.State is WebSocketState.Open or WebSocketState.CloseSent)
            {
                message.SetLength(0);
                ValueWebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(buffer.AsMemory(), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        RequestClose(RealtimeProtocol.CloseCodes.Normal, "client_closed");
                        return;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        RequestClose(RealtimeProtocol.CloseCodes.UnsupportedData, "text_only");
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (message.Length > _options.MaxInboundMessageBytes)
                    {
                        RequestClose(RealtimeProtocol.CloseCodes.MessageTooBig, "message_too_big");
                        return;
                    }
                }
                while (!result.EndOfMessage);

                Volatile.Write(ref _lastInboundTicks, _clock.GetTimestamp());
                if (Volatile.Read(ref _closeRequested) == 0 && !AdmitInboundMessage())
                {
                    RequestClose(RealtimeProtocol.CloseCodes.PolicyViolation, "rate_limited");
                    return;
                }

                if (Volatile.Read(ref _closeRequested) == 1)
                {
                    continue; // closing: drain until the client's close frame
                }

                var parsed = RealtimeProtocol.ParseClientMessage(message.GetBuffer().AsSpan(0, (int)message.Length));
                await HandleAsync(parsed, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
        {
            RequestClose(RealtimeProtocol.CloseCodes.Normal, "transport_closed");
        }
    }

    /// <summary>Fixed one-second window limiter for inbound frames.</summary>
    private bool AdmitInboundMessage()
    {
        var now = _clock.GetTimestamp();
        if (_clock.GetElapsedTime(_rateWindowStartTicks, now) >= TimeSpan.FromSeconds(1))
        {
            _rateWindowStartTicks = now;
            _rateWindowCount = 0;
        }

        return ++_rateWindowCount <= _options.MaxInboundMessagesPerSecond;
    }

    private async Task HandleAsync(RealtimeProtocol.ClientMessage message, CancellationToken token)
    {
        switch (message)
        {
            case RealtimeProtocol.SubscribeMessage subscribe:
                await SubscribeAsync(subscribe, token).ConfigureAwait(false);
                break;
            case RealtimeProtocol.UnsubscribeMessage unsubscribe:
                await UnsubscribeAsync(unsubscribe.SubscriptionId).ConfigureAwait(false);
                break;
            case RealtimeProtocol.PingMessage ping:
                Enqueue(RealtimeProtocol.Pong(ping.SentAt));
                break;
            case RealtimeProtocol.PongMessage:
                break;
            case RealtimeProtocol.InvalidMessage invalid:
                Enqueue(RealtimeProtocol.SubscriptionError(invalid.SubscriptionId, invalid.Code));
                break;
        }
    }

    // ── Subscriptions ───────────────────────────────────────

    private async Task SubscribeAsync(RealtimeProtocol.SubscribeMessage message, CancellationToken token)
    {
        await _subscriptionGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_subscriptions.TryGetValue(message.SubscriptionId, out var existing))
            {
                Enqueue(existing.Scope == message.Scope
                    ? RealtimeProtocol.Subscribed(existing.Id, existing.Scope)
                    : RealtimeProtocol.SubscriptionError(message.SubscriptionId, RealtimeProtocol.ErrorCodes.SubscriptionConflict));
                return;
            }

            if (_subscriptions.Values.Any(s => s.Scope == message.Scope))
            {
                Enqueue(RealtimeProtocol.SubscriptionError(message.SubscriptionId, RealtimeProtocol.ErrorCodes.SubscriptionConflict));
                return;
            }

            if (_subscriptions.Count >= _options.MaxSubscriptionsPerConnection)
            {
                Enqueue(RealtimeProtocol.SubscriptionError(message.SubscriptionId, RealtimeProtocol.ErrorCodes.SubscriptionLimit));
                return;
            }

            var authorization = await _authorizer.AuthorizeAsync(_principal, message.Scope, token).ConfigureAwait(false);
            switch (authorization.Outcome)
            {
                case RealtimeAuthorizationOutcome.Unauthenticated:
                    RequestClose(RealtimeProtocol.CloseCodes.CredentialsInvalid, "credentials_invalid");
                    return;
                case RealtimeAuthorizationOutcome.Unavailable:
                    Enqueue(RealtimeProtocol.SubscriptionError(message.SubscriptionId, RealtimeProtocol.ErrorCodes.AuthorizationUnavailable));
                    return;
                case RealtimeAuthorizationOutcome.Granted when authorization.Grant is { } grant:
                    await AddSubscriptionAsync(message, grant, token).ConfigureAwait(false);
                    return;
                default:
                    _logger.LogInformation(
                        "Realtime subscription denied connection {ConnectionId} scope {ScopeKind} {ScopeId}",
                        ConnectionId, message.Scope.WireKind, message.Scope.Id);
                    Enqueue(RealtimeProtocol.SubscriptionError(message.SubscriptionId, RealtimeProtocol.ErrorCodes.SubscriptionDenied));
                    return;
            }
        }
        finally
        {
            _subscriptionGate.Release();
        }
    }

    private async Task AddSubscriptionAsync(
        RealtimeProtocol.SubscribeMessage message, RealtimeSubscriptionGrant grant, CancellationToken token)
    {
        await EnsureAccessSignalAsync(grant.WorkspaceId, token).ConfigureAwait(false);
        var subscription = new Subscription(message.SubscriptionId, message.Scope, grant.WorkspaceId);
        subscription.Handle = await _source.SubscribeAsync(
            message.Scope, notification => OnNotification(subscription, notification), token).ConfigureAwait(false);

        lock (_subscriptions)
        {
            _subscriptions[subscription.Id] = subscription;
        }

        // Events are delivered strictly after the ack: take the subscription lock so
        // no event can be queued ahead of `subscribed`.
        lock (subscription.Sync)
        {
            Enqueue(RealtimeProtocol.Subscribed(subscription.Id, subscription.Scope));
            subscription.Acknowledged = true;
        }

        _logger.LogDebug(
            "Realtime subscription added connection {ConnectionId} scope {ScopeKind} {ScopeId}",
            ConnectionId, message.Scope.WireKind, message.Scope.Id);
    }

    /// <summary>One access-signal subscription per workspace of this connection, held until the connection ends.</summary>
    private async Task EnsureAccessSignalAsync(Guid workspaceId, CancellationToken token)
    {
        if (_accessSource is null || _accessHandles.ContainsKey(workspaceId))
        {
            return;
        }

        _accessHandles[workspaceId] = await _accessSource.SubscribeAccessAsync(
            workspaceId, signal => OnAccessSignal(signal), token).ConfigureAwait(false);
    }

    /// <summary>
    /// Someone's access changed: when it concerns this person, re-authorize the affected subscriptions now (not
    /// at the next periodic pass). Never blocks the transport callback; failures fail closed at the next pass.
    /// </summary>
    private ValueTask OnAccessSignal(RealtimeNotification signal)
    {
        if (Volatile.Read(ref _closeRequested) == 1)
        {
            return ValueTask.CompletedTask;
        }

        if (signal.SubjectUserId is { } subject && subject != _principal.UserId)
        {
            return ValueTask.CompletedTask;
        }

        var boardOnly = string.Equals(signal.ResourceKind, RealtimeAccessSignal.BoardResource, StringComparison.Ordinal)
            ? signal.ResourceId
            : (Guid?)null;
        _ = Task.Run(async () =>
        {
            try
            {
                await ReauthorizeAsync(
                    _stop.Token,
                    s => s.WorkspaceId == signal.WorkspaceId
                        && (boardOnly is null || (s.Scope.Kind == RealtimeScopeKind.Board && s.Scope.Id == boardOnly))).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Realtime access re-authorization failed connection {ConnectionId}", ConnectionId);
            }
        });
        return ValueTask.CompletedTask;
    }

    private async Task UnsubscribeAsync(string subscriptionId)
    {
        await _subscriptionGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!await RemoveSubscriptionAsync(subscriptionId).ConfigureAwait(false))
            {
                Enqueue(RealtimeProtocol.SubscriptionError(subscriptionId, RealtimeProtocol.ErrorCodes.UnknownSubscription));
                return;
            }

            Enqueue(RealtimeProtocol.Unsubscribed(subscriptionId));
        }
        finally
        {
            _subscriptionGate.Release();
        }
    }

    private async Task<bool> RemoveSubscriptionAsync(string subscriptionId)
    {
        Subscription? subscription;
        lock (_subscriptions)
        {
            if (!_subscriptions.Remove(subscriptionId, out subscription))
            {
                return false;
            }
        }

        subscription.Cancelled = true;
        if (subscription.Handle is not null)
        {
            await subscription.Handle.DisposeAsync().ConfigureAwait(false);
        }

        return true;
    }

    private async Task ReleaseSubscriptionsAsync()
    {
        foreach (var handle in _accessHandles.Values)
        {
            try
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Realtime access signal release failed connection {ConnectionId}", ConnectionId);
            }
        }

        _accessHandles.Clear();

        string[] ids;
        lock (_subscriptions)
        {
            ids = [.. _subscriptions.Keys];
        }

        foreach (var id in ids)
        {
            try
            {
                await RemoveSubscriptionAsync(id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Realtime subscription release failed connection {ConnectionId}", ConnectionId);
            }
        }
    }

    private ValueTask OnNotification(Subscription subscription, RealtimeNotification notification)
    {
        if (subscription.Cancelled)
        {
            return ValueTask.CompletedTask;
        }

        // Defense in depth: never forward a notification that is not for the exact
        // stream and tenant the subscription was authorized for.
        if (notification.WorkspaceId != subscription.WorkspaceId
            || !string.Equals(notification.StreamKey, subscription.Scope.StreamKey, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Realtime notification dropped: scope mismatch connection {ConnectionId} scope {ScopeKind} {ScopeId} event {EventId}",
                ConnectionId, subscription.Scope.WireKind, subscription.Scope.Id, notification.EventId);
            return ValueTask.CompletedTask;
        }

        lock (subscription.Sync)
        {
            if (subscription.Acknowledged && !subscription.Cancelled)
            {
                Enqueue(RealtimeProtocol.Event(subscription.Id, notification));
            }
        }

        return ValueTask.CompletedTask;
    }

    // ── Maintenance: heartbeat, idle, lifetime, re-authorization ──

    private async Task MaintenanceLoopAsync(CancellationToken token)
    {
        var tick = TimeSpan.FromTicks(Math.Max(
            TimeSpan.FromMilliseconds(10).Ticks,
            new[] { _options.PingInterval, _options.ReauthorizationInterval, _options.IdleTimeout }.Min().Ticks / 4));
        using var timer = new PeriodicTimer(tick, _clock);
        var lastPing = _clock.GetTimestamp();
        var lastReauthorization = lastPing;

        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                var now = _clock.GetTimestamp();

                var idle = _clock.GetElapsedTime(Volatile.Read(ref _lastInboundTicks), now);
                if (idle > _options.IdleTimeout)
                {
                    RequestClose(RealtimeProtocol.CloseCodes.HeartbeatTimeout, "heartbeat_timeout");
                    return;
                }

                var utcNow = _clock.GetUtcNow();
                if (utcNow - _startedAt >= _options.MaxConnectionLifetime)
                {
                    RequestClose(RealtimeProtocol.CloseCodes.GoingAway, "max_lifetime");
                    return;
                }

                if (_principal.ExpiresAt is { } expiresAt && utcNow >= expiresAt)
                {
                    RequestClose(RealtimeProtocol.CloseCodes.CredentialsInvalid, "credentials_expired");
                    return;
                }

                if (_clock.GetElapsedTime(lastPing, now) >= _options.PingInterval)
                {
                    lastPing = now;
                    Enqueue(RealtimeProtocol.Ping(utcNow));
                }

                if (_clock.GetElapsedTime(lastReauthorization, now) >= _options.ReauthorizationInterval)
                {
                    lastReauthorization = now;
                    await ReauthorizeAsync(token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Re-validates the credential and every subscription. Staleness of a revoked
    /// membership or permission is bounded by <see cref="RealtimeGatewayOptions.ReauthorizationInterval"/>.
    /// Authorization-system failure fails closed.
    /// </summary>
    internal async Task ReauthorizeAsync(CancellationToken token, Func<Subscription, bool>? only = null)
    {
        if (!await _credentials.IsStillValidAsync(_principal, token).ConfigureAwait(false))
        {
            RequestClose(RealtimeProtocol.CloseCodes.CredentialsInvalid, "credentials_revoked");
            return;
        }

        await _subscriptionGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Subscription[] snapshot;
            lock (_subscriptions)
            {
                snapshot = [.. _subscriptions.Values];
            }

            foreach (var subscription in snapshot.Where(s => only is null || only(s)))
            {
                var result = await _authorizer.AuthorizeAsync(_principal, subscription.Scope, token).ConfigureAwait(false);
                if (result.Outcome == RealtimeAuthorizationOutcome.Unauthenticated)
                {
                    RequestClose(RealtimeProtocol.CloseCodes.CredentialsInvalid, "credentials_invalid");
                    return;
                }

                var stillGranted = result is { Outcome: RealtimeAuthorizationOutcome.Granted, Grant: { } grant }
                    && grant.WorkspaceId == subscription.WorkspaceId;
                if (stillGranted)
                {
                    continue;
                }

                await RemoveSubscriptionAsync(subscription.Id).ConfigureAwait(false);
                Enqueue(RealtimeProtocol.SubscriptionError(
                    subscription.Id,
                    result.Outcome == RealtimeAuthorizationOutcome.Unavailable
                        ? RealtimeProtocol.ErrorCodes.AuthorizationUnavailable
                        : RealtimeProtocol.ErrorCodes.SubscriptionRevoked));
                _logger.LogInformation(
                    "Realtime subscription revoked connection {ConnectionId} scope {ScopeKind} {ScopeId} outcome {Outcome}",
                    ConnectionId, subscription.Scope.WireKind, subscription.Scope.Id, result.Outcome);
            }
        }
        finally
        {
            _subscriptionGate.Release();
        }
    }

    internal sealed class Subscription(string id, RealtimeScope scope, Guid workspaceId)
    {
        public string Id { get; } = id;
        public RealtimeScope Scope { get; } = scope;
        public Guid WorkspaceId { get; } = workspaceId;
        public object Sync { get; } = new();
        public IAsyncDisposable? Handle { get; set; }
        public volatile bool Acknowledged;
        public volatile bool Cancelled;
    }
}
