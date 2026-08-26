using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public sealed class RequestContractBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IIdempotencyExecutionContext _idempotencyContext;

    public RequestContractBehavior(
        IRequestDescriptorRegistry descriptors,
        IEnumerable<IValidator<TRequest>> validators,
        IIdempotencyExecutionContext idempotencyContext)
    {
        _descriptors = descriptors;
        _validators = validators;
        _idempotencyContext = idempotencyContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var stage = PipelineActivitySource.Instance.StartActivity("request.contract");

        var descriptor = _descriptors.GetRequired(typeof(TRequest));

        await ValidateRequestAsync(request, cancellationToken);

        if (descriptor.Scope == ApplicationScopeKind.Token)
        {
            var token = descriptor.RequestType.GetProperty("Token")!.GetValue(request) as string;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new SecurityMisconfigurationException(
                    $"{descriptor.RequestType.Name} has an empty or null Token. " +
                    "Token-scoped requests must provide a non-empty token.");
            }
        }

        if (descriptor.IsIdempotent)
        {
            _idempotencyContext.RequireKey();
        }

        if (descriptor.RequiresExpectedVersion
            && request is IExpectedVersionRequest expectedVersionRequest
            && expectedVersionRequest.ExpectedVersion <= 0)
        {
            throw new ValidationException(
                $"ExpectedVersion must be a positive value for {descriptor.RequestType.Name}. " +
                $"Request {descriptor.RequestType.Name} provides ExpectedVersion={expectedVersionRequest.ExpectedVersion}.");
        }

        return await next();
    }

    private async Task ValidateRequestAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return;
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(result => result.Errors).Where(error => error is not null).ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
