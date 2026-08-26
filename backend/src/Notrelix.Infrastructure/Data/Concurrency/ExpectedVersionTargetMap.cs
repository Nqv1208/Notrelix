using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterGroup;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterSorts;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;

namespace Notrelix.Infrastructure.Data.Concurrency;

/// <summary>
/// Sole bridge from a concrete <see cref="IExpectedVersionRequest"/> type to the
/// aggregate CLR type its expected-version constraint protects, plus the
/// resource kind the request must declare. Immutable after startup; no I/O.
/// A versioned request without an entry here fails closed at binding time and is
/// rejected by the executable completeness test in ExpectedVersionArchitectureTests.
/// </summary>
public sealed class ExpectedVersionTargetMap
{
    public sealed record TargetDefinition(string ExpectedResourceKind, Type AggregateType);

    private readonly IReadOnlyDictionary<Type, TargetDefinition> _targets;

    public static ExpectedVersionTargetMap Default { get; } = new(BuildDefault());

    public ExpectedVersionTargetMap(IReadOnlyDictionary<Type, TargetDefinition> targets)
    {
        foreach (var (requestType, definition) in targets)
        {
            if (!typeof(AggregateRoot).IsAssignableFrom(definition.AggregateType))
            {
                throw new InvalidOperationException(
                    $"ExpectedVersion target for {requestType.Name} must derive from AggregateRoot " +
                    $"(got {definition.AggregateType.FullName}).");
            }
        }

        _targets = new Dictionary<Type, TargetDefinition>(targets);
    }

    public TargetDefinition Resolve(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return _targets.TryGetValue(requestType, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"No expected-version target mapping is registered for {requestType.FullName}.");
    }

    public bool Contains(Type requestType) => _targets.ContainsKey(requestType);

    public IReadOnlyCollection<KeyValuePair<Type, TargetDefinition>> Entries => [.. _targets];

    private const string ApprovalRequestKind = "work-management.approval-request";
    private const string BoardKind = "work-management.board";
    private const string BoardFieldKind = "work-management.board-field";
    private const string BoardGroupKind = "work-management.board-group";
    private const string BoardItemKind = "work-management.board-item";
    private const string FormKind = "work-management.form";
    private const string SavedFilterKind = "work-management.saved-filter";
    private const string WorkspaceKind = "workspaces.workspace";

    private static Dictionary<Type, TargetDefinition> BuildDefault() => new(new Dictionary<Type, TargetDefinition>
    {
        // Work Management — approvals
        [typeof(ApproveApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(RejectApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(CancelApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(DeleteApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),

        // Work Management — boards / fields / groups / items
        [typeof(UpdateBoardCommand)] = new(BoardKind, typeof(Board)),
        [typeof(UpdateBoardFieldCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(DeleteBoardGroupCommand)] = new(BoardGroupKind, typeof(BoardGroup)),
        [typeof(UpdateBoardItemCommand)] = new(BoardItemKind, typeof(BoardItem)),

        // Work Management — forms
        [typeof(UpdateFormDetailsCommand)] = new(FormKind, typeof(Form)),
        [typeof(PublishFormCommand)] = new(FormKind, typeof(Form)),
        [typeof(CloseFormCommand)] = new(FormKind, typeof(Form)),
        [typeof(DeleteFormCommand)] = new(FormKind, typeof(Form)),

        // Work Management — saved filters (board views)
        [typeof(RenameSavedFilterCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(DeleteSavedFilterCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterFiltersCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterSortsCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterVisibilityCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterGroupCommand)] = new(SavedFilterKind, typeof(SavedFilter)),

        // Workspaces
        [typeof(ArchiveWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UnarchiveWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(RestoreWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(TransferOwnershipCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(DeleteWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UpdateWorkspaceProfileCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UpdateWorkspaceSettingsCommand)] = new(WorkspaceKind, typeof(Workspace)),
    });

    /// <summary>Validates the declared resource kind against the mapped expectation.</summary>
    public bool MatchesKind(Type requestType, ResourceRef resource)
    {
        var definition = Resolve(requestType);
        return string.Equals(definition.ExpectedResourceKind, resource.Kind.Value, StringComparison.Ordinal);
    }
}
