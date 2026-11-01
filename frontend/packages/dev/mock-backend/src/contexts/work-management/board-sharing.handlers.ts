/**
 * Work Management — Board sharing (the share dialog's reads and writes).
 *
 * Operations:
 *   boards.sharing              — GET    /boards/:boardId/sharing
 *   boards.sharing.makePrivate  — POST   /boards/:boardId/sharing/private
 *   boards.members.updateRole   — PATCH  /boards/:boardId/members/:userId/role
 *   boards.members.remove       — DELETE /boards/:boardId/members/:userId
 *   resources.permissions.grant — POST   /resources/:resourceType/:resourceId/permissions
 *   resources.permissions.revoke— DELETE /resources/:resourceType/:resourceId/permissions/:permissionId
 *
 * The rules mirror the producer: entries are disclosed to a Board manager or
 * member; a Guest is capped at Viewer and shown as capped, never refused;
 * nothing expires; the log records added, changed, removed, private and
 * visibility changes; the owner and the caller keep their own access.
 */

import { defineMockOperation } from "../../operations/types";
import {
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  boardSharing,
  effectiveLevel,
  levelOfRole,
  type MockBoardRoleName,
  type MockShareEntry,
  type ShareLevel,
} from "../../state/mock-board-sharing";
import type { MockStore } from "../../state/mock-store";
import type { MockBoardRecord } from "../../state/records";
import type { MockHttpResult } from "../../transport/create-response";
import { canManageBoard, canReadBoard } from "./boards.handlers";

const BOARD_KIND = "work-management.board";
const LEVELS: readonly ShareLevel[] = [
  "viewer",
  "commenter",
  "editor",
  "manager",
];
const ROLES: readonly MockBoardRoleName[] = [
  "Admin",
  "Member",
  "Observer",
  "Guest",
];

type Guarded =
  | { readonly board: MockBoardRecord; readonly refusal: null }
  | { readonly board: null; readonly refusal: MockHttpResult<unknown> };

/** The Board the caller manages, or the answer that refuses the write. */
function managedBoard(store: MockStore, boardId: string): Guarded {
  const board = store.getBoard(boardId);
  if (!board || !canReadBoard(board.id, store))
    return { board: null, refusal: notFound("Board not found") };
  if (!canManageBoard(board.id, store))
    return { board: null, refusal: forbidden() };
  return { board, refusal: null };
}

function nameOf(store: MockStore, entry: MockShareEntry): string | null {
  return entry.kind === "team"
    ? (store.spaces.getTeam(entry.teamId ?? "")?.name ?? null)
    : (store.getUser(entry.userId ?? "")?.name ?? null);
}

/** Keeps the Board's access read in step with the entry after a write. */
function syncAccess(
  store: MockStore,
  boardId: string,
  entry: MockShareEntry,
): void {
  if (!entry.userId) return;
  const present = store.sharing.entriesOf(boardId).includes(entry);
  if (present)
    store.setBoardGrant(
      boardId,
      entry.userId,
      effectiveLevel(entry),
      store.getCurrentUser().id,
    );
  else store.removeBoardGrant(boardId, entry.userId);
}

function logChange(
  store: MockStore,
  boardId: string,
  kind: "added" | "changed" | "removed",
  entry: MockShareEntry,
  from: ShareLevel | null,
  to: ShareLevel | null,
): void {
  store.sharing.pushLog(boardId, {
    kind,
    actorId: store.getCurrentUser().id,
    target: nameOf(store, entry),
    from,
    to,
    guest: entry.guest,
    at: store.getClock().isoNow(),
  });
}

function dropIfEmpty(
  store: MockStore,
  boardId: string,
  entry: MockShareEntry,
): void {
  if (entry.grantLevel !== null || entry.memberRole !== null) return;
  const list = store.sharing.entriesOf(boardId);
  const at = list.indexOf(entry);
  if (at >= 0) list.splice(at, 1);
}

const fixedEntry = (store: MockStore, entry: MockShareEntry) =>
  entry.owner || entry.userId === store.getCurrentUser().id;

export const boardSharingOperations = [
  defineMockOperation<{ boardId: string }, never, unknown>({
    id: "boards.sharing",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.GetSharing",
    },
    method: "GET",
    route: "/boards/:boardId/sharing",
    async handle({ params, store }) {
      const board = store.getBoard(params.boardId);
      if (!board || !canReadBoard(board.id, store))
        return notFound("Board not found");
      return ok(boardSharing(store, board.id, canManageBoard(board.id, store)));
    },
  }),

  defineMockOperation<{ boardId: string }, { addUserIds?: string[] }, void>({
    id: "boards.sharing.makePrivate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.MakePrivate",
    },
    method: "POST",
    route: "/boards/:boardId/sharing/private",
    async handle({ params, body, store }) {
      const { board, refusal } = managedBoard(store, params.boardId);
      if (!board) return refusal;
      const actor = store.getCurrentUser().id;
      const list = store.sharing.entriesOf(board.id);
      for (const userId of body?.addUserIds ?? []) {
        if (list.some((entry) => entry.userId === userId)) continue;
        const entry: MockShareEntry = {
          id: store.sharing.nextId(),
          kind: "person",
          userId,
          teamId: null,
          level: "editor",
          grantLevel: null,
          memberRole: "Member",
          guest: false,
          owner: false,
          addedAt: store.getClock().isoNow(),
        };
        list.push(entry);
        store.setBoardGrant(board.id, userId, "editor", actor);
        logChange(store, board.id, "added", entry, null, "editor");
      }
      store.setBoardVisibility(board.id, "private");
      store.sharing.pushLog(board.id, {
        kind: "private",
        actorId: actor,
        target: null,
        from: null,
        to: null,
        guest: false,
        at: store.getClock().isoNow(),
      });
      return noContent();
    },
  }),

  defineMockOperation<
    { boardId: string; userId: string },
    { role?: string },
    void
  >({
    id: "boards.members.updateRole",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.UpdateMemberRole",
    },
    method: "PATCH",
    route: "/boards/:boardId/members/:userId/role",
    async handle({ params, body, store }) {
      const { board, refusal } = managedBoard(store, params.boardId);
      if (!board) return refusal;
      const entry = store.sharing
        .entriesOf(board.id)
        .find((item) => item.userId === params.userId && item.memberRole);
      if (!entry || !entry.memberRole) return notFound("Member not found");
      const role = ROLES.find((candidate) => candidate === body?.role);
      if (!role) return validationError({ role: ["Unknown role."] });
      if (fixedEntry(store, entry))
        return validationError({
          role: ["The owner's and your own permission cannot be changed."],
        });
      const from = entry.level;
      entry.memberRole = role;
      entry.level = effectiveLevel(entry);
      syncAccess(store, board.id, entry);
      logChange(store, board.id, "changed", entry, from, levelOfRole(role));
      return noContent();
    },
  }),

  defineMockOperation<{ boardId: string; userId: string }, never, void>({
    id: "boards.members.remove",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.RemoveMember",
    },
    method: "DELETE",
    route: "/boards/:boardId/members/:userId",
    async handle({ params, store }) {
      const { board, refusal } = managedBoard(store, params.boardId);
      if (!board) return refusal;
      const entry = store.sharing
        .entriesOf(board.id)
        .find((item) => item.userId === params.userId && item.memberRole);
      if (!entry) return notFound("Member not found");
      if (fixedEntry(store, entry))
        return validationError({
          entry: ["The owner and you keep access to the Board."],
        });
      const from = entry.level;
      entry.memberRole = null;
      entry.level = effectiveLevel(entry);
      dropIfEmpty(store, board.id, entry);
      syncAccess(store, board.id, entry);
      logChange(store, board.id, "removed", entry, from, null);
      return noContent();
    },
  }),

  defineMockOperation<
    { resourceType: string; resourceId: string },
    {
      subjectType?: string;
      subjectId?: string;
      level?: string;
      expectedVersion?: number | null;
    },
    void
  >({
    id: "resources.permissions.grant",
    contract: {
      kind: "openapi",
      operationId: "Governance.ResourcePermissions.Grant",
    },
    method: "POST",
    route: "/resources/:resourceType/:resourceId/permissions",
    async handle({ params, body, store }) {
      if (params.resourceType !== BOARD_KIND) return notFound();
      const { board, refusal } = managedBoard(store, params.resourceId);
      if (!board) return refusal;
      const level = LEVELS.find((candidate) => candidate === body?.level);
      if (!level) return validationError({ level: ["Unknown permission."] });
      const team = body?.subjectType === "Team";
      if (!team && body?.subjectType !== "User")
        return validationError({ subjectType: ["Unknown subject."] });
      const subjectId = body?.subjectId ?? "";
      const list = store.sharing.entriesOf(board.id);
      const existing = list.find((item) =>
        team ? item.teamId === subjectId : item.userId === subjectId,
      );
      if (existing && fixedEntry(store, existing))
        return validationError({
          level: ["The owner's and your own permission cannot be changed."],
        });
      if (existing && existing.grantLevel === level) return noContent();
      if (existing && existing.grantLevel !== null) {
        // Changing a grant's level names the version read with it.
        if (body?.expectedVersion !== (existing.version ?? 1))
          return {
            status: 412,
            body: {
              type: "https://notrelix.dev/problems/common.precondition-failed",
              title: "Precondition failed",
              status: 412,
              detail:
                "The access entry changed since it was read. Reload and try again.",
              errorCode: "governance.permission.version-conflict",
            },
          };
      }
      if (existing) {
        const from = existing.level;
        existing.version = (existing.version ?? 1) + 1;
        existing.grantLevel = level;
        existing.level = effectiveLevel(existing);
        syncAccess(store, board.id, existing);
        logChange(store, board.id, "changed", existing, from, level);
        return noContent();
      }
      const entry: MockShareEntry = {
        id: store.sharing.nextId(),
        kind: team ? "team" : "person",
        userId: team ? null : subjectId,
        teamId: team ? subjectId : null,
        level,
        grantLevel: level,
        memberRole: null,
        guest: false,
        owner: false,
        addedAt: store.getClock().isoNow(),
      };
      list.push(entry);
      syncAccess(store, board.id, entry);
      logChange(store, board.id, "added", entry, null, level);
      return noContent();
    },
  }),

  defineMockOperation<
    { resourceType: string; resourceId: string; permissionId: string },
    never,
    void
  >({
    id: "resources.permissions.revoke",
    contract: {
      kind: "openapi",
      operationId: "Governance.ResourcePermissions.Revoke",
    },
    method: "DELETE",
    route: "/resources/:resourceType/:resourceId/permissions/:permissionId",
    async handle({ params, store }) {
      if (params.resourceType !== BOARD_KIND) return notFound();
      const { board, refusal } = managedBoard(store, params.resourceId);
      if (!board) return refusal;
      const entry = store.sharing
        .entriesOf(board.id)
        .find((item) => item.id === params.permissionId && item.grantLevel);
      if (!entry) return notFound("Permission not found");
      if (fixedEntry(store, entry))
        return validationError({
          entry: ["The owner and you keep access to the Board."],
        });
      const from = entry.level;
      entry.grantLevel = null;
      entry.level = effectiveLevel(entry);
      dropIfEmpty(store, board.id, entry);
      syncAccess(store, board.id, entry);
      if (entry.memberRole)
        logChange(store, board.id, "changed", entry, from, entry.level);
      else logChange(store, board.id, "removed", entry, from, null);
      return noContent();
    },
  }),
];
