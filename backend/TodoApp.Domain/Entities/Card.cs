using TodoApp.Domain.Common;

namespace TodoApp.Domain.Entities;

public class Card : AuditableEntity
{
    public Guid ListId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? DescriptionMd { get; private set; }
    public double Position { get; private set; }
    public string? Priority { get; private set; }
    public string Status { get; private set; } = "open";
    public DateTime? DueDate { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Cover { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }

    public BoardList List { get; private set; } = null!;

    private Card() : base() { }

    public static Card Create(Guid listId, Guid createdBy, string title, double position = 0)
    {
        return new Card
        {
            ListId = listId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Position = position,
            Status = "open"
        };
    }
}
