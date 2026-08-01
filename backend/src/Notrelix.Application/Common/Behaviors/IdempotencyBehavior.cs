using System.Text.Json;

namespace Notrelix.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResultExpiry = TimeSpan.FromHours(24);

    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyStore idempotencyStore,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyStore = idempotencyStore;
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
                _logger.LogDebug("Idempotency replay for {Operation} key={Key}", identity.Operation, identity.Key);
                return DeserializeResult(beginResult.SerializedResult!);

            case IdempotencyBeginStatus.InProgress:
                throw new ConflictException(
                    $"Request with idempotency key '{identity.Key}' is already being processed.");

            case IdempotencyBeginStatus.PayloadMismatch:
                throw new ConflictException(
                    $"Idempotency key '{identity.Key}' was already used with a different request payload.");

            case IdempotencyBeginStatus.Started:
                break;

            default:
                throw new InvalidOperationException($"Unknown idempotency status: {beginResult.Status}");
        }

        var response = await next();

        var serialized = JsonSerializer.Serialize(response);
        var resultType = typeof(TResponse).AssemblyQualifiedName!;

        await _idempotencyStore.CompleteAsync(
            identity,
            beginResult.LeaseToken,
            serialized,
            resultType,
            DateTimeOffset.UtcNow.Add(ResultExpiry),
            cancellationToken);

        return response;
    }

    private static IdempotencyIdentity BuildIdentity(IIdempotentRequest request)
    {
        var operation = request.GetType().FullName ?? request.GetType().Name;

        var scope = request switch
        {
            IWorkspaceRequest ws => $"workspace:{ws.WorkspaceId}",
            IAccountRequest => "account",
            _ => "global"
        };

        var requestHash = ComputeRequestHash(request);

        return new IdempotencyIdentity(operation, scope, request.IdempotencyKey, requestHash);
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

    private static TResponse DeserializeResult(string serialized)
    {
        return JsonSerializer.Deserialize<TResponse>(serialized)
            ?? throw new InvalidOperationException("Failed to deserialize cached idempotency result.");
    }
}
