/**
 * The target-Space checks a Board or a root Doc move shares (GOV-028): the
 * Space must exist in the workspace and be visible (404 otherwise), the
 * caller needs Editor there (403), and an archived Space takes nothing (409
 * `workspaces.space.inactive`). `null` (the workspace level) needs no check.
 */
import { forbidden, notFound } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import { accessAtLeast } from "../../state/mock-spaces";

export function targetSpaceDenied(
  store: MockStore,
  workspaceId: string,
  spaceId: string | null,
) {
  if (!spaceId) return null;
  const space = store.spaces.getSpace(spaceId);
  const access = space
    ? store.spaces.accessFor(spaceId, store.spaceViewer(workspaceId))
    : null;
  if (!space || space.workspaceId !== workspaceId || access === null)
    return notFound();
  if (!accessAtLeast(access, "Editor")) return forbidden();
  if (space.archived) {
    return {
      status: 409,
      body: {
        type: "https://docs.notrelix.com/problems/workspaces-space-inactive",
        title: "Conflict",
        status: 409,
        detail: "The target space is archived.",
        errorCode: "workspaces.space.inactive",
      },
    };
  }
  return null;
}

/** The producer's 412 for a stale `expectedVersion` (common.precondition-failed). */
export function preconditionFailed() {
  return {
    status: 412,
    body: {
      type: "https://docs.notrelix.com/problems/common-precondition-failed",
      title: "Precondition failed",
      status: 412,
      detail: "The resource was modified by another request. Reload and retry.",
      errorCode: "common.precondition-failed",
    },
  };
}
