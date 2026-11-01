using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Public.ActivityAttribution;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Automation;

/// <summary>Automation-owned implementation of the rule attribution Collaboration needs for an Item's history.</summary>
public sealed class AutomationActivityFactsAdapter : IAutomationActivityFacts
{
    private const int MaxNameLength = 120;

    private readonly IAutomationDbContext _context;

    public AutomationActivityFactsAdapter(IAutomationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> FindRuleNameAsync(Guid workspaceId, Guid executionId, CancellationToken cancellationToken)
    {
        var name = await (
            from e in _context.AutomationExecutions.AsNoTracking()
            join r in _context.AutomationRules.AsNoTracking().IgnoreQueryFilters() on e.RuleId equals r.Id
            where e.Id == executionId && e.WorkspaceId == workspaceId
            select r.Name)
            .FirstOrDefaultAsync(cancellationToken);
        return name is null || name.Length <= MaxNameLength ? name : name[..MaxNameLength];
    }
}
