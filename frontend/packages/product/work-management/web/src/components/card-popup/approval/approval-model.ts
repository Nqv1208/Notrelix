import type { Board } from "@notrelix/work-management-core";
import type { ItemPagePeople } from "../../item-page/item-page-types";

/** How the server refused an approval write: someone answered first, not allowed, or anything else. */
export function approvalFailure(
  error: unknown,
): "conflict" | "forbidden" | "failed" | null {
  if (error === null || error === undefined) return null;
  const kind = (error as { kind?: string }).kind;
  return kind === "conflict"
    ? "conflict"
    : kind === "forbidden"
      ? "forbidden"
      : "failed";
}

/**
 * The people who may be asked to approve the item: whoever can read the board (its members, and everyone
 * when it is workspace-wide), never the viewer.
 */
export function approvalCandidates(
  board: Board,
  people: ItemPagePeople | undefined,
  viewerUserId: string | null | undefined,
): { id: string; label: string }[] {
  const candidates = people
    ? people.people.filter(
        (person) => person.onBoard || board.visibility === "workspace",
      )
    : board.members;
  return candidates
    .filter((person) => person.userId !== viewerUserId && person.name)
    .map((person) => ({ id: person.userId, label: person.name }));
}
