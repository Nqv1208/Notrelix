namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// Neighbor-relative placement (SPEC WMREQ131): both sibling ids absent appends to the
/// group; the server generates the persisted position. The item's own facts (description, start
/// and due date) may be given at creation so the item never exists half-filled; people, labels and
/// field values are other facts and are written through their own routes.
/// </summary>
public record CreateBoardItemRequest(
    Guid GroupId,
    string Title,
    Guid? PreviousSiblingId,
    Guid? NextSiblingId,
    string? DescriptionMd = null,
    DateTime? StartDate = null,
    DateTime? DueDate = null);
