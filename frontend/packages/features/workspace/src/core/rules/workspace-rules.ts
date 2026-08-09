/**
 * Workspace domain rules.
 *
 * Pure, stateless business logic for workspace governance constraints.
 * These are frontend projections of the backend invariants and must
 * stay in sync with the backend domain model.
 *
 * No framework dependencies. No async. No side effects.
 */

export interface RemoveOwnerResult {
  canRemove: boolean;
  error?: string;
}

/**
 * Validates whether a workspace owner can be removed.
 *
 * Invariant: A workspace must always have at least one owner.
 *
 * @param ownerCount - Total number of owners currently in the workspace
 */
export function validateRemoveOwner(ownerCount: number): RemoveOwnerResult {
  if (ownerCount <= 1) {
    return {
      canRemove: false,
      error: "Cannot remove the last owner of a workspace",
    };
  }
  return { canRemove: true };
}

/**
 * Validates whether a workspace owner's role can be downgraded.
 *
 * Invariant: The last owner cannot have their role downgraded.
 *
 * @param ownerCount - Total number of owners currently in the workspace
 */
export function validateDowngradeOwner(ownerCount: number): RemoveOwnerResult {
  if (ownerCount <= 1) {
    return {
      canRemove: false,
      error: "Cannot downgrade the last owner of a workspace",
    };
  }
  return { canRemove: true };
}
