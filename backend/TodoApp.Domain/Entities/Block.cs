using System.Text.Json;
using TodoApp.Domain.Common;

namespace TodoApp.Domain.Entities;

public class Block : AuditableEntity
{
    public Guid PageId { get; private set; }
    public Guid? ParentBlockId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Type { get; private set; } = "paragraph";
    public string Properties { get; private set; } = "{}";
    public double Position { get; private set; }
    public bool IsDeleted { get; private set; }
    public int Version { get; private set; } = 1;

    public Page Page { get; private set; } = null!;
    public Block? ParentBlock { get; private set; }

    private readonly List<Block> _children = [];
    public IReadOnlyCollection<Block> Children => _children.AsReadOnly();

    private Block() : base() { }

    public static Block Create(
        Guid pageId,
        Guid createdByUserId,
        string type,
        string properties = "{}",
        double position = 0,
        Guid? parentBlockId = null)
    {
        ValidateJson(properties);

        return new Block
        {
            PageId = pageId,
            ParentBlockId = parentBlockId,
            CreatedByUserId = createdByUserId,
            Type = string.IsNullOrWhiteSpace(type) ? "paragraph" : type.Trim(),
            Properties = properties,
            Position = position,
            IsDeleted = false,
            Version = 1
        };
    }

    public void UpdateType(string type)
    {
        Type = string.IsNullOrWhiteSpace(type) ? Type : type.Trim();
        Version++;
    }

    public void UpdateProperties(string properties)
    {
        ValidateJson(properties);
        Properties = properties;
        Version++;
    }

    public void Move(double newPosition, Guid? newParentBlockId = null)
    {
        Position = newPosition;
        ParentBlockId = newParentBlockId;
        Version++;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Version++;
    }

    private static void ValidateJson(string value)
    {
        try
        {
            JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
        }
        catch (JsonException)
        {
            throw new ArgumentException("Properties phai la JSON hop le.", nameof(value));
        }
    }
}
