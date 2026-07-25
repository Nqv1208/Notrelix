namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Rule codes for Guard and Common (lifecycle + scope) contexts.
/// SharedKernel codes live in SharedKernelRuleCodes.
/// Context-specific codes live in their respective bounded context RuleCodes.
/// </summary>
public static class CommonRuleCodes
{
    // ── Guard ─────────────────────────────────────────────────────────────
    public const string Guard_Null = "Guard_Null";
    public const string Guard_NullOrWhiteSpace = "Guard_NullOrWhiteSpace";
    public const string Guard_Empty = "Guard_Empty";
    public const string Guard_Positive = "Guard_Positive";
    public const string Guard_NotNegative = "Guard_NotNegative";
    public const string Guard_MaxLength = "Guard_MaxLength";
    public const string Guard_InRange = "Guard_InRange";

    // ── Common / Lifecycle ────────────────────────────────────────────────
    public const string Common_EntityHasBeenDeleted = "Common_EntityHasBeenDeleted";
    public const string Common_InvalidDeletionTime = "Common_InvalidDeletionTime";
    public const string Common_InvalidRestoreTime = "Common_InvalidRestoreTime";
    public const string Common_EntityAlreadyDeleted = "Common_EntityAlreadyDeleted";
    public const string Common_EntityNotDeleted = "Common_EntityNotDeleted";
    public const string Common_EntityDeleted = "Common_EntityDeleted";
    public const string Common_Audit_InvalidTimestamp = "Common_Audit_InvalidTimestamp";
    public const string Common_Audit_CreatedAtAlreadySet = "Common_Audit_CreatedAtAlreadySet";
    public const string Common_Audit_UpdatedAtBeforeCreatedAt = "Common_Audit_UpdatedAtBeforeCreatedAt";
    public const string Common_Audit_EmptyActor = "Common_Audit_EmptyActor";
    public const string Common_Audit_UpdatedAtRegression = "Common_Audit_UpdatedAtRegression";

    // ── Common / Scope ────────────────────────────────────────────────────
    public const string Common_WorkspaceScopeMismatch = "Common_WorkspaceScopeMismatch";
    public const string Common_BoardScopeMismatch = "Common_BoardScopeMismatch";
    public const string Common_ChildNotFound = "Common_ChildNotFound";
}