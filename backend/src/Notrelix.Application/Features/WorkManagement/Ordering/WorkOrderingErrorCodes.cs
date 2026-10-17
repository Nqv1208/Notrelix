namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>Stable public error codes of the Work ordering contract (SPEC WMREQ138).</summary>
public static class WorkOrderingErrorCodes
{
    /// <summary>The declared neighbors are not the current first/last/adjacent siblings; reload and rebase.</summary>
    public const string StalePlacement = "work.ordering.stale-placement";

    /// <summary>A named ordering uniqueness constraint rejected the write; reload and rebase.</summary>
    public const string Conflict = Notrelix.Application.Common.Data.IOrderingConflictClassifier.ConflictCode;
}
