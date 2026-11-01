using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Application.Features.Operators.Public.Audit;

namespace Notrelix.Application.Features.Operators.Audit;

internal sealed class OperatorChangeCommit : IOperatorChangeCommit
{
    private readonly IOperatorsDbContext _context;

    public OperatorChangeCommit(IOperatorsDbContext context) => _context = context;

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was changed by someone else. Reload and retry.");
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).Contains("23505", StringComparison.Ordinal)
            || (ex.InnerException?.Message ?? ex.Message).Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("A record with the same identity already exists.");
        }
    }
}
