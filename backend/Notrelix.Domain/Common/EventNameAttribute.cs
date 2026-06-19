namespace Notrelix.Domain.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventNameAttribute : Attribute
{
    public string Name { get; }
    public int Version { get; init; } = 1;

    public EventNameAttribute(string name)
    {
        Name = name;
    }
}
