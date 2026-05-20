using System.Text.Json;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Boards;

public class BoardColumn : AuditableEntity
{
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public string Settings { get; private set; } = "{}";
    public double Position { get; private set; }
    public bool IsHidden { get; private set; }

    public Board Board { get; private set; } = null!;

    private BoardColumn() : base() { }

    public static BoardColumn Create(
        Guid boardId,
        string name,
        string fieldType,
        string settings = "{}",
        double position = 0,
        bool isHidden = false)
    {
        ValidateJson(settings);

        return new BoardColumn
        {
            BoardId = boardId,
            Name = string.IsNullOrWhiteSpace(name) ? "Column" : name.Trim(),
            FieldType = string.IsNullOrWhiteSpace(fieldType) ? "text" : fieldType.Trim().ToLowerInvariant(),
            Settings = string.IsNullOrWhiteSpace(settings) ? "{}" : settings,
            Position = position,
            IsHidden = isHidden
        };
    }

    public void Update(string name, string fieldType, string settings)
    {
        ValidateJson(settings);

        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        FieldType = string.IsNullOrWhiteSpace(fieldType) ? FieldType : fieldType.Trim().ToLowerInvariant();
        Settings = string.IsNullOrWhiteSpace(settings) ? "{}" : settings;
    }

    public void UpdatePosition(double position) => Position = position;
    public void Hide() => IsHidden = true;
    public void Show() => IsHidden = false;

    public static IReadOnlyList<BoardColumn> CreateDefaults(Guid boardId)
    {
        var definitions = new[]
        {
            ("Status", "select", ToJson(new { options = new[] { "Open", "In Progress", "In Review", "Done" } })),
            ("Priority", "select", ToJson(new { options = new[] { "Low", "Medium", "High", "Urgent" } })),
            ("Assignee", "people", ToJson(new { multiple = true })),
            ("Due date", "date", ToJson(new { includeTime = false })),
            ("Estimate", "number", ToJson(new { format = "hours" })),
            ("Blocked", "checkbox", ToJson(new { defaultValue = false }))
        };

        return definitions.Select((definition, index) => Create(
            boardId,
            definition.Item1,
            definition.Item2,
            definition.Item3,
            (index + 1) * 1024d)).ToList();
    }

    private static void ValidateJson(string value)
    {
        try
        {
            JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Settings must be valid JSON.", nameof(value), ex);
        }
    }

    private static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }
}
