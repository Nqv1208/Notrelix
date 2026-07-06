using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Automation.Abstractions;

public interface IAutomationDbContext
{
    DbSet<AutomationRule> AutomationRules { get; }
    DbSet<AutomationExecution> AutomationExecutions { get; }
    DbSet<ScheduledJob> ScheduledJobs { get; }
    DbSet<AutomationTemplate> AutomationTemplates { get; }
    DbSet<AiAgent> AiAgents { get; }
    DbSet<AiAgentRun> AiAgentRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}