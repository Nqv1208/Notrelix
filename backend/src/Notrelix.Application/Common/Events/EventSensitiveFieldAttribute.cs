namespace Notrelix.Application.Common.Events;

/// <summary>
/// Explicit security-content classification for a public integration-event
/// payload property that is intentional credential-adjacent delivery material
/// rather than personal data (P13-EVT-002A / IAREQ094).
///
/// Raw secrets remain hard-forbidden in public payloads; this attribute exists
/// so protected delivery material (e.g. encrypted single-use link tokens) is
/// intentionally classified instead of silently serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EventSensitiveFieldAttribute : Attribute
{
    public string PropertyName { get; set; }
    public string Classification { get; set; }
    public string Justification { get; set; }

    public EventSensitiveFieldAttribute(string propertyName)
    {
        PropertyName = propertyName;
        Classification = string.Empty;
        Justification = string.Empty;
    }
}
