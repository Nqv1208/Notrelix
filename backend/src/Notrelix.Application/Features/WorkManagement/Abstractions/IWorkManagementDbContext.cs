namespace Notrelix.Application.Features.WorkManagement.Abstractions;

public interface IWorkManagementDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<BoardGroup> BoardGroups { get; }
    DbSet<BoardField> BoardFields { get; }
    DbSet<FieldOption> FieldOptions { get; }
    DbSet<BoardView> BoardViews { get; }
    DbSet<BoardItem> BoardItems { get; }
    DbSet<BoardItemValue> BoardItemValues { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<Label> Labels { get; }
}
