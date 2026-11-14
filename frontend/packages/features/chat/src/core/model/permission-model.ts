/**
 * The role matrix of a channel's permissions (UC-CHT-H01): framework-neutral rules the settings dialog shows. Rows are the actions,
 * columns the roles, and each cell is one of three states: the default of the role, an allow, or a deny. The server is the authority
 * (it resolves, validates and answers 422 where a rule is broken); these rules only keep the matrix from offering what it would refuse.
 */

import {
  CHAT_CHANNEL_ACTIONS,
  CHAT_PERMISSION_SUBJECTS,
  type ChatChannelAction,
  type ChatPermissionEntry,
  type ChatPermissionSubject,
} from "../types/chat";

export type ChatPermissionCell = "default" | "allow" | "deny";

/** The state of every cell, keyed by `${subject}:${action}`; a missing key is the default. */
export type ChatPermissionCells = Readonly<Record<string, ChatPermissionCell>>;

export const permissionCellKey = (
  subject: ChatPermissionSubject,
  action: ChatChannelAction,
): string => `${subject}:${action}`;

/** The cells of a stored set. */
export function cellsOf(
  entries: readonly ChatPermissionEntry[],
): ChatPermissionCells {
  const cells: Record<string, ChatPermissionCell> = {};
  for (const entry of entries) {
    cells[permissionCellKey(entry.subject, entry.action)] = entry.effect;
  }
  return cells;
}

export function cellOf(
  cells: ChatPermissionCells,
  subject: ChatPermissionSubject,
  action: ChatChannelAction,
): ChatPermissionCell {
  return cells[permissionCellKey(subject, action)] ?? "default";
}

/** The set to send: one entry per non-default cell, in the order of the roles and then the actions. */
export function entriesOf(cells: ChatPermissionCells): ChatPermissionEntry[] {
  const entries: ChatPermissionEntry[] = [];
  for (const subject of CHAT_PERMISSION_SUBJECTS) {
    for (const action of CHAT_CHANNEL_ACTIONS) {
      const cell = cellOf(cells, subject, action);
      if (cell !== "default") entries.push({ subject, action, effect: cell });
    }
  }
  return entries;
}

export function withCell(
  cells: ChatPermissionCells,
  subject: ChatPermissionSubject,
  action: ChatChannelAction,
  cell: ChatPermissionCell,
): ChatPermissionCells {
  const next: Record<string, ChatPermissionCell> = { ...cells };
  const key = permissionCellKey(subject, action);
  if (cell === "default") delete next[key];
  else next[key] = cell;
  return next;
}

/** Whether two matrices hold the same overrides. */
export function sameCells(
  a: ChatPermissionCells,
  b: ChatPermissionCells,
): boolean {
  return CHAT_PERMISSION_SUBJECTS.every((subject) =>
    CHAT_CHANNEL_ACTIONS.every(
      (action) => cellOf(a, subject, action) === cellOf(b, subject, action),
    ),
  );
}

/**
 * The actions a Workspace role does not hold by default (the Chat cells of the Governance baseline). An allow above the role is
 * refused by the server (422), so the matrix does not offer it. A channel manager stands for the Member role.
 */
const ROLE_LACKS: Readonly<
  Record<ChatPermissionSubject, readonly ChatChannelAction[]>
> = {
  manager: ["broadcast"],
  member: ["broadcast"],
  guest: ["pin", "broadcast", "invite"],
};

/** Whether the matrix offers an allow for the cell (the role holds the action by default). */
export function allowOffered(
  subject: ChatPermissionSubject,
  action: ChatChannelAction,
): boolean {
  return !ROLE_LACKS[subject].includes(action);
}

/** Whether the matrix offers a deny for the cell: a channel manager cannot be denied inviting people (422 permission-lockout). */
export function denyOffered(
  subject: ChatPermissionSubject,
  action: ChatChannelAction,
): boolean {
  return !(subject === "manager" && action === "invite");
}

/** The matrix of a read-only channel: members and guests cannot post, managers keep posting. */
export function readOnlyCells(cells: ChatPermissionCells): ChatPermissionCells {
  return withCell(
    withCell(cells, "member", "post", "deny"),
    "guest",
    "post",
    "deny",
  );
}

/** Whether the matrix makes the channel read-only for members and guests. */
export function isReadOnlyMatrix(cells: ChatPermissionCells): boolean {
  return (
    cellOf(cells, "member", "post") === "deny" &&
    cellOf(cells, "guest", "post") === "deny"
  );
}
