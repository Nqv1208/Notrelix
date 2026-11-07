using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.FormNotifications;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement;

/// <summary>Work Management-owned implementation of the form facts consumed by Collaboration's "new response" notification.</summary>
public sealed class FormNotificationFactsAdapter : IFormNotificationFacts
{
    private readonly IWorkManagementDbContext _context;

    public FormNotificationFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<FormNotificationFacts?> FindAsync(Guid workspaceId, Guid formId, CancellationToken cancellationToken)
    {
        var form = await _context.Forms
            .AsNoTracking()
            .Where(f => f.Id == formId && f.WorkspaceId == workspaceId && f.DeletedAt == null)
            .Select(f => new { f.BoardId, f.Name, f.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);
        if (form is null) return null;

        var boardLive = await _context.Boards
            .AsNoTracking()
            .AnyAsync(b => b.Id == form.BoardId && b.WorkspaceId == workspaceId && b.DeletedAt == null, cancellationToken);
        if (!boardLive) return null;

        // shortcut: the Board's managers are its Owner and Admin members; a Manager grant held only through
        // Governance (a Space or an explicit grant) is not read here.
        var managers = await _context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == form.BoardId && (m.Role == BoardRole.Owner || m.Role == BoardRole.Admin))
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var people = new List<Guid>();
        if (form.CreatedBy is { } creator && creator != Guid.Empty) people.Add(creator);
        people.AddRange(managers);
        return new FormNotificationFacts(form.BoardId, form.Name, people.Distinct().ToList());
    }
}
