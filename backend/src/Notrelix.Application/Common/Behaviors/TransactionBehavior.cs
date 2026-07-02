using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly IRlsSessionContext? _rlsSessionContext;

    public TransactionBehavior(
        IApplicationDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IRlsSessionContext? rlsSessionContext = null)
    {
        _context = context;
        _logger = logger;
        _rlsSessionContext = rlsSessionContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return await next();

        _logger.LogTrace("Beginning transaction for {RequestType}", typeof(TRequest).Name);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (_rlsSessionContext is not null)
            {
                await _rlsSessionContext.ApplyAsync(_context.Database, cancellationToken);
            }

            var response = await next();

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogTrace("Transaction completed for {RequestType}", typeof(TRequest).Name);

            return response;
        }
        catch
        {
            _logger.LogWarning("Transaction rolled back for {RequestType}", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
