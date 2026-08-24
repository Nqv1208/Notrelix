using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Common.Diagnostics;

namespace Notrelix.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IIdempotencyRequestFingerprint _fingerprint;
    private readonly IIdempotencyReplayPolicy _replayPolicy;
    private readonly IdempotencyPartitionFactory _partitionFactory;
    private readonly IIdempotencyExecutionContext _executionContext;
    private readonly IIdempotencyExecutionContextWriter _executionContextWriter;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private readonly PipelineMetrics _metrics;

    public IdempotencyBehavior(
        IIdempotencyStore idempotencyStore,
        IIdempotencyRequestFingerprint fingerprint,
        IIdempotencyReplayPolicy replayPolicy,
        IdempotencyPartitionFactory partitionFactory,
        IIdempotencyExecutionContext executionContext,
        IIdempotencyExecutionContextWriter executionContextWriter,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger,
        PipelineMetrics? metrics = null)
    {
        _metrics = metrics ?? new PipelineMetrics();
        _idempotencyStore = idempotencyStore;
        _fingerprint = fingerprint;
        _replayPolicy = replayPolicy;
        _partitionFactory = partitionFactory;
        _executionContext = executionContext;
        _executionContextWriter = executionContextWriter;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotentRequest)
            return await next();

        // 1. Response-type eligibility fails before Begin — no row is created for
        //    sensitive response types (e.g. token/auth responses).
        _replayPolicy.EnsureResponseTypeAllowed<TResponse>();

        // 2. Require key and construct identity.
        var identity = BuildIdentity(idempotentRequest);

        // 3. Begin.
        IdempotencyBeginResult beginResult;
        using (PipelineActivitySource.Instance.StartActivity("idempotency.acquire"))
        {
            beginResult = await _idempotencyStore.BeginAsync(identity, cancellationToken);
        }

        // 4. Replay/mismatch handling.
        switch (beginResult.Status)
        {
            case IdempotencyBeginStatus.Completed:
                _logger.LogDebug("Idempotency replay for {Operation} scope={Scope}", identity.Operation, identity.Scope);
                using (PipelineActivitySource.Instance.StartActivity("idempotency.replay"))
                {
                    _metrics.IdempotencyReplays.Add(1);
                    _executionContextWriter.MarkReplay();
                    return ReplayResult(beginResult);
                }

            case IdempotencyBeginStatus.PayloadMismatch:
                throw new IdempotencyPayloadMismatchException(identity.Operation);

            case IdempotencyBeginStatus.Started:
                break;

            default:
                throw new InvalidOperationException($"Unknown idempotency status: {beginResult.Status}");
        }

        // 5. Execute handler.
        var response = await next();

        // 6. Serialize with the replay contract options (Result envelopes and
        // enums must round-trip, spec 3.7).
        var serialized = JsonSerializer.Serialize(response, IdempotencyJson.Options);

        // 7. Serialized-result eligibility fails before Complete — the request
        //    transaction rolls back instead of leaving a non-replayable Started row.
        _replayPolicy.EnsureSerializedResultAllowed(response, serialized);

        // 8. Complete. The store owns the expiry calculation.
        var resultContract = identity.Operation;
        using (PipelineActivitySource.Instance.StartActivity("idempotency.complete"))
        {
            await _idempotencyStore.CompleteAsync(
                identity,
                serialized,
                resultContract,
                cancellationToken);
        }

        // 9. Return.
        return response;
    }

    private IdempotencyIdentity BuildIdentity(IIdempotentRequest request)
    {
        var operation = IdempotencyOperationMetadata.Resolve<TRequest>();
        var scope = _partitionFactory.BuildPartition(request);
        var keyHash = HashRawKey(_executionContext.RequireKey());
        var requestHash = _fingerprint.Compute(request, typeof(TRequest));

        return new IdempotencyIdentity(operation, scope, keyHash, requestHash);
    }

    private static string HashRawKey(string rawKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawKey);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes);
    }

    private static TResponse ReplayResult(IdempotencyBeginResult beginResult)
    {
        var expectedContract = IdempotencyOperationMetadata.Resolve<TRequest>();

        if (beginResult.ResultContract is not null
            && beginResult.ResultContract != expectedContract)
        {
            throw new ConflictException(
                $"Idempotency result contract mismatch. Expected '{expectedContract}' but stored '{beginResult.ResultContract}'.");
        }

        return JsonSerializer.Deserialize<TResponse>(beginResult.SerializedResult!, IdempotencyJson.Options)
            ?? throw new InvalidOperationException("Failed to deserialize cached idempotency result.");
    }
}
