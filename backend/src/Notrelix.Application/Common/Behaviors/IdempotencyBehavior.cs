using System.Text.Json;
using Notrelix.Application.Common.Context;

namespace Notrelix.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResultExpiry = TimeSpan.FromHours(24);

    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyStore idempotencyStore,
        ICurrentTenantContext tenantContext,
        TimeProvider timeProvider,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyStore = idempotencyStore;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotentRequest)
            return await next();

        var identity = BuildIdentity(idempotentRequest);

        var beginResult = await _idempotencyStore.BeginAsync(identity, LeaseDuration, cancellationToken);

        switch (beginResult.Status)
        {
            case IdempotencyBeginStatus.Completed:
                _logger.LogDebug("Idempotency replay for {Operation} scope={Scope}", identity.Operation, identity.Scope);
                return ReplayResult(beginResult);

            case IdempotencyBeginStatus.InProgress:
                throw new ConflictException(
                    $"Request with idempotency key is already being processed for operation '{identity.Operation}'.");

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
        var resultContract = typeof(TResponse).FullName!;

        await _idempotencyStore.CompleteAsync(
            identity,
            beginResult.LeaseToken,
            serialized,
            resultContract,
            _timeProvider.GetUtcNow().Add(ResultExpiry),
            cancellationToken);

        return response;
    }

    private IdempotencyIdentity BuildIdentity(IIdempotentRequest request)
    {
        var operation = typeof(TRequest).FullName ?? typeof(TRequest).Name;

        var scope = BuildQualifiedScope(request);

        var requestHash = ComputeRequestHash(request);

        return new IdempotencyIdentity(operation, scope, request.IdempotencyKey, requestHash);
    }

    private string BuildQualifiedScope(IIdempotentRequest request)
    {
        if (request is IWorkspaceRequest ws)
        {
            return $"workspace:{ws.WorkspaceId}";
        }

        if (request is IAccountRequest)
        {
            var accountId = _tenantContext.AccountId
                ?? throw new InvalidOperationException("AccountId not resolved for account-scoped idempotent request.");
            return $"account:{accountId}";
        }

        if (_tenantContext.IsSystemContext)
        {
            return $"system:{typeof(TRequest).Name}";
        }

        var userId = _tenantContext.UserId
            ?? throw new InvalidOperationException("UserId not resolved for global idempotent request.");
        return $"global:user:{userId}";
    }

    private static string ComputeRequestHash(object request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(bytes)[..32];
    }

    private static TResponse ReplayResult(IdempotencyBeginResult beginResult)
    {
        var expectedContract = typeof(TResponse).FullName!;

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
