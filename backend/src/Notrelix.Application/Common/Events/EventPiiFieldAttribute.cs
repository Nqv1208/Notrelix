namespace Notrelix.Application.Common.Events;

/// <summary>
/// Explicit PII classification metadata for a public integration-event payload
/// property (IAREQ086 / IAREQ132 / P13-EVT-002B).
///
/// A PII-bearing public event MUST classify each personal-data field with its
/// semantic purpose and the consumer justification explaining why stable IDs +
/// approved read contracts are insufficient. Unnecessary mutable PII should be
/// removed rather than classified.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EventPiiFieldAttribute : Attribute
{
    public string PropertyName { get; set; }
    public string Purpose { get; set; }
    public string ConsumerJustification { get; set; }

    public EventPiiFieldAttribute(string propertyName)
    {
        PropertyName = propertyName;
        Purpose = string.Empty;
        ConsumerJustification = string.Empty;
    }
}
