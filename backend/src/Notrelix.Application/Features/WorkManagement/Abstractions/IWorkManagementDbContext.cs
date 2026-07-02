using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Formulas;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Rollups;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Workload;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.WorkManagement.Abstractions;

public interface IWorkManagementDbContext
{
    // Board core
    DbSet<Board> Boards { get; }
    DbSet<BoardGroup> BoardGroups { get; }
    DbSet<BoardField> BoardFields { get; }
    DbSet<FieldOption> FieldOptions { get; }
    DbSet<BoardView> BoardViews { get; }
    DbSet<BoardViewPin> BoardViewPins { get; }
    DbSet<BoardViewUserPreference> BoardViewUserPreferences { get; }
    DbSet<SavedFilter> SavedFilters { get; }
    DbSet<BoardItem> BoardItems { get; }
    DbSet<BoardItemValue> BoardItemValues { get; }
    DbSet<BoardItemMember> BoardItemMembers { get; }
    DbSet<BoardItemLabel> BoardItemLabels { get; }
    DbSet<BoardItemLink> BoardItemLinks { get; }
    DbSet<Label> Labels { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<BoardSubscriber> BoardSubscribers { get; }

    // Relations
    DbSet<BoardRelation> BoardRelations { get; }
    DbSet<BoardItemConnection> BoardItemConnections { get; }
    DbSet<MirrorValueSnapshot> MirrorValueSnapshots { get; }
    DbSet<ItemDependency> ItemDependencies { get; }
    DbSet<TimeTrackingEntry> TimeTrackingEntries { get; }
    DbSet<RelationFieldConfig> RelationFieldConfigs { get; }

    // Formulas & Rollups
    DbSet<FormulaDependency> FormulaDependencies { get; }
    DbSet<RollupSnapshot> RollupSnapshots { get; }

    // Checklists
    DbSet<Checklist> Checklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }

    // Forms
    DbSet<Form> Forms { get; }
    DbSet<FormQuestion> FormQuestions { get; }
    DbSet<FormSubmission> FormSubmissions { get; }

    // Approvals
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }

    // Workload
    DbSet<WorkloadAllocation> WorkloadAllocations { get; }

    // Templates
    DbSet<BoardTemplate> BoardTemplates { get; }
    DbSet<ItemTemplate> ItemTemplates { get; }
}
