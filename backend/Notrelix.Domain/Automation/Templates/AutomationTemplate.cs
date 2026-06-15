using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Templates;

public class AutomationTemplate : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Category { get; private set; } = null!;
    public JsonValue Definition { get; private set; } = null!;
    public AutomationTemplateStatus Status { get; private set; }

    private AutomationTemplate() : base() { }

    public static AutomationTemplate Create(string name, string category, JsonValue definition, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(category);
        Guard.NotNull(definition);

        var template = new AutomationTemplate
        {
            Name = name.Trim(),
            Category = category.Trim(),
            Definition = definition,
            Status = AutomationTemplateStatus.Published
        };

        template.SetAuditOnCreate(createdBy, createdAt);
        template.AddDomainEvent(new Events.AutomationTemplateCreatedEvent(
            Guid.Empty, template.Id, template.Name, createdAt));

        return template;
    }
}
