namespace Notrelix.Domain.Documents.Versions;

/// <summary>Why a history entry exists: taken by the system, saved by a person with a name, or written by a restore.</summary>
public enum DocumentVersionKind
{
    Automatic,
    Named,
    Restore
}
