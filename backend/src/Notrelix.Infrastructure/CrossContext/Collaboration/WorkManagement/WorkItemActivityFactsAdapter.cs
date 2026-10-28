using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement;

/// <summary>Work Management-owned implementation of the facts Collaboration needs to word an Item's history.</summary>
public sealed class WorkItemActivityFactsAdapter : IWorkItemActivityFacts
{
    private readonly IWorkManagementDbContext _context;

    public WorkItemActivityFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<WorkFieldActivityFacts?> FindFieldAsync(Guid workspaceId, Guid fieldId, CancellationToken cancellationToken)
    {
        var field = await _context.BoardFields
            .AsNoTracking()
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.WorkspaceId == workspaceId, cancellationToken);
        if (field is null) return null;

        var isStatus = false;
        if (field.Type == FieldType.Status)
        {
            var statusFields = await _context.BoardFields
                .AsNoTracking()
                .Where(f => f.BoardId == field.BoardId && f.Type == FieldType.Status && f.DeletedAt == null)
                .ToListAsync(cancellationToken);
            isStatus = Application.Features.WorkManagement.Common.WorkItemSemanticFields
                .ResolveStatusField(statusFields)?.Id == field.Id;
        }

        return new WorkFieldActivityFacts(
            field.Name, isStatus, ValueModeOf(field), field.Options.ToDictionary(o => o.Id, o => o.Name));
    }

    /// <summary>Values of sensitive fields, long text, people, links and computed fields are never copied into the history.</summary>
    private static WorkFieldValueMode ValueModeOf(BoardField field) =>
        field.IsSensitive
            ? WorkFieldValueMode.Hidden
            : field.Type switch
            {
                FieldType.Status or FieldType.Select => WorkFieldValueMode.Option,
                FieldType.MultiSelect => WorkFieldValueMode.OptionList,
                FieldType.Text or FieldType.Number or FieldType.Date or FieldType.Checkbox => WorkFieldValueMode.Plain,
                _ => WorkFieldValueMode.Hidden,
            };

    public Task<string?> FindBoardNameAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken) =>
        _context.Boards.AsNoTracking()
            .Where(b => b.Id == boardId && b.WorkspaceId == workspaceId)
            .Select(b => (string?)b.Title)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<string?> FindGroupNameAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken) =>
        _context.BoardGroups.AsNoTracking()
            .Where(g => g.Id == groupId && g.WorkspaceId == workspaceId)
            .Select(g => (string?)g.Title)
            .FirstOrDefaultAsync(cancellationToken);
}
