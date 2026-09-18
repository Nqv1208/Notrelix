using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// Test-only Automation context on EF InMemory. The invalid-config dispatch
/// proof needs the real EF query + change tracker to honor the Queued → Running
/// progression the use case performs before parsing the rule configuration; a
/// mocked DbSet cannot map the aggregate. Kept deliberately narrow: InMemory is
/// used for orchestration semantics only, never to prove PostgreSQL/RLS.
/// </summary>
public sealed class TestAutomationDbContext(DbContextOptions<TestAutomationDbContext> options)
    : DbContext(options), IAutomationDbContext
{
    public DbSet<AutomationRule> AutomationRules { get; set; } = null!;
    public DbSet<AutomationExecution> AutomationExecutions { get; set; } = null!;
    public DbSet<ScheduledJob> ScheduledJobs { get; set; } = null!;
    public DbSet<AutomationTemplate> AutomationTemplates { get; set; } = null!;
    public DbSet<AiAgent> AiAgents { get; set; } = null!;
    public DbSet<AiAgentRun> AiAgentRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AutomationRule>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.OwnsOne(x => x.Configuration, config =>
            {
                config.OwnsOne(c => c.Trigger);
                config.OwnsOne(c => c.Action);
                config.OwnsOne(c => c.Condition);
            });
        });

        modelBuilder.Entity<AutomationExecution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.ExecutionId);
        });

        modelBuilder.Entity<ScheduledJob>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.OwnsOne(x => x.Schedule);
        });

        modelBuilder.Entity<AutomationTemplate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Definition);
        });

        modelBuilder.Entity<AiAgent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.ModelPolicy);
            entity.Ignore(x => x.Instruction);
            entity.Ignore(x => x.ToolPermissions);
        });

        modelBuilder.Entity<AiAgentRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Input);
            entity.Ignore(x => x.Output);
            entity.Ignore(x => x.Error);
        });
    }
}