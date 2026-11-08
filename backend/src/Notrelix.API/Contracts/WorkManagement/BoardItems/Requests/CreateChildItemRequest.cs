namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// A sub-item's own facts (description, start and due date) may be given at creation, the same as a
/// board item's, so it never exists half-filled. People, labels and field values are written after.
/// </summary>
public record CreateChildItemRequest(
    string Title,
    string? DescriptionMd = null,
    DateTime? StartDate = null,
    DateTime? DueDate = null);
