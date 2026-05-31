// Template: CQRS Command Handler
// Replace placeholders: {{EntityName}}, {{DomainName}}, {{CommandName}}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.{{DomainName}}.DTOs;
using Notrelix.Domain.Entities.{{DomainName}};
using Notrelix.Domain.Exceptions;

namespace Notrelix.Application.Features.{{DomainName}}.Commands;

public class {{CommandName}}Handler : IRequestHandler<{{CommandName}}, {{EntityName}}Dto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public {{CommandName}}Handler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<{{EntityName}}Dto> Handle({{CommandName}} request, CancellationToken cancellationToken)
    {
        // TODO: Validate prerequisites (e.g., parent entity exists)

        // TODO: Create entity
        var entity = new {{EntityName}}
        {
            // Set properties from request
            CreatedBy = _currentUser.UserId
        };

        _context.{{EntityName}}s.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Return DTO
        return new {{EntityName}}Dto(
            entity.Id,
            // Map other properties
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
