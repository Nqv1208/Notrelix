// Template: CQRS Query Handler
// Replace placeholders: {{EntityName}}, {{DomainName}}, {{QueryName}}, {{ReturnType}}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.{{DomainName}}.DTOs;
using Notrelix.Domain.Exceptions;

namespace Notrelix.Application.Features.{{DomainName}}.Queries;

public class {{QueryName}}Handler : IRequestHandler<{{QueryName}}, {{ReturnType}}>
{
    private readonly IApplicationDbContext _context;

    public {{QueryName}}Handler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<{{ReturnType}}> Handle({{QueryName}} request, CancellationToken cancellationToken)
    {
        // TODO: Validate entity exists
        var entityExists = await _context.{{EntityName}}s
            .AnyAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        if (!entityExists)
            throw new NotFoundException($"{{EntityName}} with id '{request.Id}' not found");

        // TODO: Query data
        var results = await _context.{{EntityName}}s
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.Position)  // Or other ordering
            .Select(e => new {{EntityName}}Dto(
                e.Id,
                // Map properties
                e.CreatedAt,
                e.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return results;
    }
}
