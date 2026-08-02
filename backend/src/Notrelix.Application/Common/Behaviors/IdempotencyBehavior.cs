using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Notrelix.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IIdempotencyRequestFingerprint _fingerprint;
    private readonly IIdempotencyReplayPolicy _replayPolicy;
    private readonly IdempotencyPartitionFactory _partitionFactory;
    private readonly IdempotencyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyStore idempotencyStore,
        IIdempotencyRequestFingerprint fingerprint,
        IIdempotencyReplayPolicy replayPolicy,
        IdempotencyPartitionFactory partitionFactory,
        IOptions<IdempotencyOptions> options,
        TimeProvider timeProvider,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyStore = idempotencyStore;
        _fingerprint = fingerprint;
        _replayPolicy = replayPolicy;
        _partitionFactory = partitionFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotentRequest)
            return await next();

        var identity = BuildIdentity(idempotentRequest);

        var beginResult = await _idempotencyStore.BeginAsync(identity, cancellationToken);

        switch (beginResult.Status)
        {
            case IdempotencyBeginStatus.Completed:
                _logger.LogDebug("Idempotency replay for {Operation} scope={Scope}", identity.Operation, identity.Scope);
                return ReplayResult(beginResult);

            case IdempotencyBeginStatus.PayloadMismatch:
                throw new ConflictException(
                    $"Idempotency key was already used with a different request payload for operation '{identity.Operation}'.");

            case IdempotencyBeginStatus.Started:
                break;

            default:
                throw new InvalidOperationException($"Unknown idempotency status: {beginResult.Status}");
        }

        var response = await next();

        var serialized = JsonSerializer.Serialize(response);

        if (_replayPolicy.CanCacheResult(response, serialized))
        {
            var resultContract = identity.Operation;

            await _idempotencyStore.CompleteAsync(
                identity,
                serialized,
                resultContract,
                _timeProvider.GetUtcNow().Add(_options.ResultExpiry),
                cancellationToken);
        }
        else
        {
            _logger.LogDebug(
                "Idempotency result not cached for {Operation} (policy rejected). Request remains non-replayable.",
                identity.Operation);
        }

        return response;
    }

    private IdempotencyIdentity BuildIdentity(IIdempotentRequest request)
    {
        var operation = IdempotencyOperationMetadata.Resolve<TRequest>();
        var scope = _partitionFactory.BuildPartition(request);
        var keyHash = HashRawKey(request.IdempotencyKey);
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

        return JsonSerializer.Deserialize<TResponse>(beginResult.SerializedResult!)
            ?? throw new InvalidOperationException("Failed to deserialize cached idempotency result.");
    }
}
