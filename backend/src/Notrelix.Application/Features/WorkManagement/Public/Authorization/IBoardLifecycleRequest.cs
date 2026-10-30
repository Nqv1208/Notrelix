namespace Notrelix.Application.Features.WorkManagement.Public.Authorization;

/// <summary>
/// Work Management-published marker for the Board lifecycle commands that must stay
/// reachable on an archived or deleted Board: unarchive, delete and restore. Governance
/// refuses every other write on an archived Board or on one of its children
/// (<see cref="WorkManagementDenialCodes.BoardArchived"/>, GOV-033) and reads this marker
/// to let these through.
/// </summary>
public interface IBoardLifecycleRequest;

/// <summary>
/// The Board lifecycle command that reaches a deleted Board: restore. A deleted Board is
/// NotFound to every other request, whoever asks (GOV-025 step 1).
/// </summary>
public interface IBoardRestoreRequest : IBoardLifecycleRequest;

/// <summary>Stable codes of the Forbidden decisions that follow a Board or Group being archived.</summary>
public static class WorkManagementDenialCodes
{
    /// <summary>A write on an archived Board, or on one of its children, by a person who may otherwise make it.</summary>
    public const string BoardArchived = "WorkManagement_Board_Archived";

    /// <summary>A write on an Item (or its checklist) inside an archived Group, by a person who may otherwise make it.</summary>
    public const string GroupArchived = "WorkManagement_Group_Archived";
}
