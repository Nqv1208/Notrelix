using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Domain.Workspaces.Rules;

/// <summary>
/// Space hierarchy and lifecycle invariants (docs/product/workspaces.md
/// WSP-021, WSP-022). Every check works on Application-supplied facts.
/// </summary>
public static class SpaceRules
{
    public static void ValidateName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
    }

    /// <summary>Sibling names are unique, case-insensitively, under one parent.</summary>
    public static void EnsureUniqueAmongSiblings(string name, IReadOnlyCollection<string> siblingNames)
    {
        if (siblingNames.Any(sibling => string.Equals(sibling.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_DuplicateSiblingName,
                $"A space named '{name}' already exists at this level.");
    }

    /// <summary>
    /// The parent must be an active Space of the same Workspace, and must not be
    /// the Space itself or one of its descendants (the Space must not appear on
    /// the parent's ancestor path).
    /// </summary>
    public static void EnsureValidParent(Guid spaceId, Guid workspaceId, SpaceParentFact parent)
    {
        if (parent.ParentWorkspaceId != workspaceId)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_ParentInOtherWorkspace,
                "The parent space belongs to another workspace.");

        if (parent.SpaceId == spaceId || parent.AncestorPathIds.Contains(spaceId))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_MoveCreatesCycle,
                "A space cannot be placed under itself or one of its descendants.");

        if (!parent.IsActive)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_ParentNotActive,
                "The parent space is archived or deleted.");
    }

    public static void EnsureCanArchive(SpaceContentFact content)
    {
        if (content.ActiveChildSpaces > 0 || content.ActiveResources > 0)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotArchiveWithActiveContent,
                "A space with active child spaces, boards or docs cannot be archived. Move or archive them first.");
    }

    public static void EnsureCanDelete(SpaceContentFact content)
    {
        if (content.ExistingChildSpaces > 0 || content.ExistingResources > 0)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotDeleteWithContent,
                "A space that still contains child spaces, boards or docs cannot be deleted. Move or delete them first.");
    }

    public static void EnsureParentActiveForReactivation(bool parentIsActive)
    {
        if (!parentIsActive)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_ParentMustBeActiveToReactivate,
                "The parent space must be active before this space can be unarchived or restored.");
    }
}
