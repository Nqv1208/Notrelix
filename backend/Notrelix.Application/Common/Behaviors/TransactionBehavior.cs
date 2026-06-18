using MediatR;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;

namespace Notrelix.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IApplicationDbContext context, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ITransactionalRequest)
        {
            _logger.LogTrace("Beginning transaction for {RequestType}", typeof(TRequest).Name);

            var response = await next();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogTrace("Transaction completed for {RequestType}", typeof(TRequest).Name);

            return response;
        }

        return await next();
    }
}
