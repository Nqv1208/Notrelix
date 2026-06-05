using System.Text.Json;
using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Domain.Entities.Boards;

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
    public string FieldValues { get; private set; } = "{}";
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation
    public BoardList List { get; private set; } = null!;
    public Page? LinkedPage { get; private set; }

    private readonly List<CardMember> _members = new();
    public IReadOnlyCollection<CardMember> Members => _members.AsReadOnly();

    private readonly List<CardLabel> _labels = new();
    public IReadOnlyCollection<CardLabel> Labels => _labels.AsReadOnly();

    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    private Card() : base() { }

    public static Card Create(Guid listId, Guid createdBy, string title, double position = 0)
    {
        return CreateCore(listId, createdBy, title, position, boardId: null);
    }

    public static Card Create(Guid listId, Guid boardId, Guid createdBy, string title, double position = 0)
    {
        var card = CreateCore(listId, createdBy, title, position, boardId);
        card.AddDomainEvent(new CardCreatedEvent(card.Id, boardId, listId, card.Title, createdBy));
        return card;
    }

    private static Card CreateCore(Guid listId, Guid createdBy, string title, double position, Guid? boardId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Card title cannot be empty.", nameof(title));

        if (double.IsNaN(position) || double.IsInfinity(position))
            throw new ArgumentException("Position must be a finite number.", nameof(position));

        return new Card
        {
            ListId = listId,
            CreatedByUserId = createdBy,
            Title = title.Trim(),
            Position = position,
            Status = CardStatus.Open
        };
    }

    public void UpdateTitle(string title) => Rename(title, Guid.Empty);

    public void Rename(string title, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Card title cannot be empty.", nameof(title));

        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;

        Title = normalizedTitle;
        AddDomainEvent(new CardUpdatedEvent(Id, updatedBy, Title));
    }

    public void UpdateDescription(string? description) => DescriptionMd = description?.Trim();

    public void UpdatePriority(CardPriority? priority) => ChangePriority(priority, Guid.Empty);

    public void ChangePriority(CardPriority? priority, Guid changedBy)
    {
        if (Priority == priority) return;

        var oldPriority = Priority;
        Priority = priority;
        AddDomainEvent(new CardPriorityChangedEvent(Id, oldPriority, priority, changedBy));
    }

    public void UpdateCover(string? cover) => Cover = cover;
    public void ReplaceFieldValues(string? valuesJson)
    {
        FieldValues = string.IsNullOrWhiteSpace(valuesJson) ? "{}" : valuesJson;
        ValidateFieldValues(FieldValues);
    }

    public void UpdateFieldValue(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var values = GetFieldValues();
        if (value is null) values.Remove(key);
        else values[key] = value;

        FieldValues = JsonSerializer.Serialize(values);
    }

    public Dictionary<string, object?> GetFieldValues()
    {
        if (string.IsNullOrWhiteSpace(FieldValues)) return new Dictionary<string, object?>();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(FieldValues);
        return values?.ToDictionary(item => item.Key, item => NormalizeJsonElement(item.Value))
            ?? new Dictionary<string, object?>();
    }

    public void SetDueDate(DateTime? dueDate)
    {
        SetDueDate(dueDate, Guid.Empty);
    }

    public void SetDueDate(DateTime? dueDate, Guid changedBy)
    {
        if (DueDate != dueDate)
        {
            var oldDueDate = DueDate;
            DueDate = dueDate;
            AddDomainEvent(new CardDueDateChangedEvent(this.Id, oldDueDate, dueDate, changedBy));
        }
    }

    public void SetStartDate(DateTime? startDate) => StartDate = startDate;

    public void UpdateStatus(CardStatus status) => ChangeStatus(status, Guid.Empty);

    public void ChangeStatus(CardStatus status, Guid changedBy)
    {
        if (Status == status) return;

        var oldStatus = Status;
        Status = status;
        CompletedAt = status == CardStatus.Done ? DateTime.UtcNow : null;
        AddDomainEvent(new CardStatusChangedEvent(Id, oldStatus, status, changedBy));
    }

    public void LinkPage(Guid pageId)
    {
        LinkPage(pageId, Guid.Empty);
    }

    public void LinkPage(Guid pageId, Guid linkedBy)
    {
        if (LinkedPageId == pageId) return;
        LinkedPageId = pageId;
        AddDomainEvent(new CardLinkedToPageEvent(this.Id, pageId));
    }

    public void UnlinkPage() => UnlinkPage(Guid.Empty);

    public void UnlinkPage(Guid unlinkedBy)
    {
        if (!LinkedPageId.HasValue) return;

        var oldPageId = LinkedPageId.Value;
        LinkedPageId = null;
        AddDomainEvent(new CardUnlinkedFromPageEvent(Id, oldPageId, unlinkedBy));
    }

    public void Move(Guid newListId, double newPosition)
    {
        MoveToGroup(newListId, newPosition, Guid.Empty);
    }

    public void MoveToGroup(Guid newListId, double newPosition, Guid movedBy)
    {
        if (double.IsNaN(newPosition) || double.IsInfinity(newPosition))
            throw new ArgumentException("Position must be a finite number.", nameof(newPosition));

        var oldListId = ListId;

        ListId = newListId;
        Position = newPosition;

        AddDomainEvent(new CardMovedEvent(this.Id, oldListId, newListId, newPosition));
    }

    public void AssignMember(Guid userId, Guid assignedBy)
    {
        if (_members.Any(m => m.UserId == userId))
            return; // Already assigned

        var member = CardMember.Create(this.Id, userId, assignedBy);
        _members.Add(member);
        AddDomainEvent(new CardAssignedEvent(this.Id, userId, assignedBy));
    }

    public void AddLabel(Guid labelId)
    {
        if (_labels.Any(l => l.LabelId == labelId))
            return;

        var label = CardLabel.Create(this.Id, labelId);
        _labels.Add(label);
    }

    public void Archive() => Archive(Guid.Empty);

    public void Archive(Guid archivedBy)
    {
        if (IsArchived) return;
        IsArchived = true;
        AddDomainEvent(new CardArchivedEvent(Id, archivedBy));
    }

    public void Unarchive() => IsArchived = false;

    public void SoftDelete() => SoftDelete(Guid.Empty);

    public void SoftDelete(Guid deletedBy)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        AddDomainEvent(new CardDeletedEvent(Id, deletedBy));
    }

    private static void ValidateFieldValues(string valuesJson)
    {
        JsonDocument.Parse(valuesJson);
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => element.EnumerateArray().Select(NormalizeJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => NormalizeJsonElement(property.Value)),
            _ => element.ToString()
        };
    }
}
