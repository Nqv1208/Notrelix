namespace Notrelix.Domain.Common.Exceptions;

/// <summary>
/// Rule codes for SharedKernel, Guard, and Common (lifecycle + scope) contexts.
/// </summary>
public static class CommonRuleCodes
{
    // ── SharedKernel / ValueObjects ───────────────────────────────────────
    public const string SharedKernel_Json_InvalidFormat = "SharedKernel_Json_InvalidFormat";
    public const string SharedKernel_Email_InvalidFormat = "SharedKernel_Email_InvalidFormat";
    public const string SharedKernel_Url_InvalidFormat = "SharedKernel_Url_InvalidFormat";
    public const string SharedKernel_Slug_InvalidFormat = "SharedKernel_Slug_InvalidFormat";
    public const string SharedKernel_Color_InvalidFormat = "SharedKernel_Color_InvalidFormat";
    public const string SharedKernel_Money_InvalidCurrency = "SharedKernel_Money_InvalidCurrency";
    public const string SharedKernel_DateRange_StartAfterEnd = "SharedKernel_DateRange_StartAfterEnd";

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
    public const string Common_WidgetCoordinatesMustBeNonNegative = "Common_WidgetCoordinatesMustBeNonNegative";
    public const string Common_WidgetDimensionsMustBePositive = "Common_WidgetDimensionsMustBePositive";
    public const string Common_DefaultMemberRoleMustBeGuestOrMember = "Common_DefaultMemberRoleMustBeGuestOrMember";
    public const string Common_InvitationExpiryDaysOutOfRange = "Common_InvitationExpiryDaysOutOfRange";
}
