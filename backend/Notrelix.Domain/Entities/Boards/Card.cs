using System.Text.Json;
using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

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
        if (DueDate != dueDate)
        {
            DueDate = dueDate;
            AddDomainEvent(new Events.Board.CardDueDateSetEvent(this.Id, dueDate));
        }
    }

    public void SetStartDate(DateTime? startDate) => StartDate = startDate;

    public void UpdateStatus(CardStatus status)
    {
        Status = status;
        CompletedAt = status == CardStatus.Done ? DateTime.UtcNow : null;
    }

    public void LinkPage(Guid pageId)
    {
        if (LinkedPageId == pageId) return;
        LinkedPageId = pageId;
        AddDomainEvent(new Events.Board.CardLinkedToPageEvent(this.Id, pageId));
    }

    public void UnlinkPage() => LinkedPageId = null;

    public void Move(Guid newListId, double newPosition)
    {
        var oldListId = ListId;
        var oldPosition = Position;

        ListId = newListId;
        Position = newPosition;

        AddDomainEvent(new Events.Board.CardMovedEvent(this.Id, oldListId, newListId, newPosition));
    }

    public void AssignMember(Guid userId, Guid assignedBy)
    {
        if (_members.Any(m => m.UserId == userId))
            return; // Already assigned

        var member = CardMember.Create(this.Id, userId);
        _members.Add(member);
        AddDomainEvent(new Events.Board.CardAssignedEvent(this.Id, userId, assignedBy));
    }

    public void AddLabel(Guid labelId)
    {
        if (_labels.Any(l => l.LabelId == labelId))
            return;

        var label = CardLabel.Create(this.Id, labelId);
        _labels.Add(label);
    }

    public void Archive() => IsArchived = true;
    public void Unarchive() => IsArchived = false;

    public void SoftDelete()
    {
        IsDeleted = true;
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
