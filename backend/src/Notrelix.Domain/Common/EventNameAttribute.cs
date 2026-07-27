namespace Notrelix.Domain.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventNameAttribute : Attribute
{
    public string Name { get; }
    public int Version { get; init; } = 1;

    public EventNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(name), name, "Event name cannot exceed 128 characters.");

        Name = name.Trim();
    }
}
