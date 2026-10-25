namespace Notrelix.Domain.WorkManagement.Views;

public enum ViewType
{
    Table,
    Kanban,
    Calendar,
    Timeline,
    Form
}

/// <summary>
/// Who sees and edits a saved view (WM-8): a Board view is a tab every Board member sees and Editors and
/// above change; a Private view is its creator's alone.
/// </summary>
public enum BoardViewVisibility
{
    Board,
    Private
}
