import type {
  BoardSharingData,
  SharingLevel,
} from "@notrelix/work-management-web";

/** One request of the Board share dialog, addressed to a producer route that exists. */
export interface SharingRequest {
  readonly method: "POST" | "PATCH" | "DELETE";
  readonly path: string;
  readonly body?: unknown;
}

type Entry = BoardSharingData["entries"][number];

const BOARD_KIND = "work-management.board";

/** Board role names that carry a level; "commenter" has no Board role. */
const ROLE_OF_LEVEL: Partial<Record<SharingLevel, string>> = {
  viewer: "Observer",
  editor: "Member",
  manager: "Admin",
};

function permissionsPath(boardId: string): string {
  return `/resources/${BOARD_KIND}/${boardId}/permissions`;
}

function userIdOf(entry: Entry): string {
  return entry.userId ?? entry.id;
}

function isMember(entry: Entry): boolean {
  return entry.source === "member" || entry.source === "both";
}

/**
 * The writes that change one entry's level. A grant is replaced by granting the
 * subject again at the new level (the producer keeps one grant per subject); a
 * Board membership changes its Board role. A level a Board role cannot hold is
 * refused here, never guessed.
 */
export function planChangeLevel(
  boardId: string,
  entry: Entry,
  level: SharingLevel,
): SharingRequest[] {
  const requests: SharingRequest[] = [];
  const member = isMember(entry) && entry.kind !== "team";
  const role = ROLE_OF_LEVEL[level];
  if (member && entry.permissionId == null && !role) {
    throw new Error("This person's membership cannot hold that permission.");
  }
  if (!member || entry.permissionId != null) {
    requests.push({
      method: "POST",
      path: permissionsPath(boardId),
      body: {
        subjectType: entry.kind === "team" ? "Team" : "User",
        subjectId: entry.kind === "team" ? entry.id : userIdOf(entry),
        level,
      },
    });
  }
  if (member && role) {
    requests.push({
      method: "PATCH",
      path: `/boards/${boardId}/members/${userIdOf(entry)}/role`,
      body: { role },
    });
  }
  return requests;
}

/** The writes that take one entry's access away: its grant and its membership. */
export function planRemove(boardId: string, entry: Entry): SharingRequest[] {
  const requests: SharingRequest[] = [];
  if (entry.permissionId) {
    requests.push({
      method: "DELETE",
      path: `${permissionsPath(boardId)}/${entry.permissionId}`,
    });
  }
  if (isMember(entry) && entry.kind !== "team") {
    requests.push({
      method: "DELETE",
      path: `/boards/${boardId}/members/${userIdOf(entry)}`,
    });
  }
  return requests;
}

/**
 * The producer's sharing read names a person by `id` (the user id) and a Team by
 * its id; the dialog reads `userId`, `department` and `outsideCompany`.
 */
export function normalizeSharing(raw: BoardSharingData): BoardSharingData {
  return {
    ...raw,
    entries: (raw.entries ?? []).map((entry) => ({
      ...entry,
      userId:
        entry.userId !== undefined
          ? entry.userId
          : entry.kind === "team"
            ? null
            : entry.id,
      department: entry.department ?? null,
      outsideCompany: entry.outsideCompany ?? entry.kind === "guest",
    })),
    log: raw.log ?? [],
  };
}
