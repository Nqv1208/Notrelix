using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Board;

public class Card : AuditableEntity
{
    public Guid ListId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? DescriptionMd { get; private set; }
    public Guid? LinkedPageId { get; private set; }
    public double Position { get; private set; }
    public CardPriority? Priority { get; private set; }
    public CardStatus Status { get; private set; } = CardStatus.Open;
    public DateTime? DueDate { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Cover { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public BoardList List { get; private set; } = null!;
    public Document.Page? LinkedPage { get; private set; }

    private readonly List<CardMember> _members = new();
    public IReadOnlyCollection<CardMember> Members => _members.AsReadOnly();

    private readonly List<CardLabel> _labels = new();
    public IReadOnlyCollection<CardLabel> Labels => _labels.AsReadOnly();

    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    private Card() : base() { }

    public static Card Create(Guid listId, Guid createdBy, string title, double position = 0)
    {
        return new Card
        {
            ListId = listId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Position = position,
            Status = CardStatus.Open
        };
    }

    public void UpdateTitle(string title) => Title = title.Trim();
    public void UpdateDescription(string? description) => DescriptionMd = description?.Trim();
    public void UpdatePriority(CardPriority? priority) => Priority = priority;
    public void UpdateCover(string? cover) => Cover = cover;

    public void SetDueDate(DateTime? dueDate) => DueDate = dueDate;
    public void SetStartDate(DateTime? startDate) => StartDate = startDate;

    public void UpdateStatus(CardStatus status)
    {
        Status = status;
        CompletedAt = status == CardStatus.Done ? DateTime.UtcNow : null;
    }

    public void LinkPage(Guid pageId) => LinkedPageId = pageId;
    public void UnlinkPage() => LinkedPageId = null;

    public void Move(Guid listId, double position)
    {
        ListId = listId;
        Position = position;
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;

    public void SoftDelete()
    {
        IsDeleted = true;
    }
}
