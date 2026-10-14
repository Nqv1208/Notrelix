/**
 * The member's work feed (Home, My work, Inbox): reads and writes the
 * generated contract does not have. Each family is one registered gap
 * (CTR-GAP-WS-*, see `tooling/contracts/enabled-consumer-surface.ts`), with
 * the shapes the frontend needs; the backend implements against
 * `docs/superpowers/be-blockers-2026-10.md` ("BE requirement from FE").
 */
import { defineMockOperation } from "../../operations/types";
import {
  noContent,
  notFound,
  ok,
  created,
  validationError,
  forbidden,
  conflict,
} from "../../transport/create-response";
import {
  buildHomeFeed,
  buildInbox,
  buildInboxSummary,
  buildMyWorkExtended,
  bulkWrite,
  createAssignedItem,
  decideApproval,
  markAllInboxRead,
  markInbox,
  remindOwner,
  reopenItem,
  replyToMention,
  setFollowing,
  setItemBucket,
  type BulkAction,
  type FeedWriteResult,
  type WorkBucket,
} from "../../state/mock-work-feed";

const MY_WORK_EXT = "CTR-GAP-WS-MYWORK-EXT" as const;
const HOME_FEED = "CTR-GAP-WS-HOME-FEED" as const;
const INBOX = "CTR-GAP-WS-INBOX" as const;

const BUCKETS: readonly WorkBucket[] = ["todo", "doing", "review", "done"];

function outcome(result: FeedWriteResult) {
  switch (result) {
    case "ok":
      return noContent();
    case "not-found":
      return notFound();
    case "forbidden":
      return forbidden();
    default:
      return conflict("The item is not in a state that allows this.");
  }
}

function isBucket(value: unknown): value is WorkBucket {
  return BUCKETS.includes(value as WorkBucket);
}

export const workFeedOperations = [
  // ── My work: extended read ───────────────────────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.myWork.extended",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "GET",
    route: "/workspaces/:workspaceId/my-work/extended",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(buildMyWorkExtended(store, params.workspaceId));
    },
  }),

  // ── Approval decision ────────────────────────────────────────────────────
  defineMockOperation<
    { id: string },
    { decision?: string; comment?: string | null }
  >({
    id: "wsFeed.items.approval",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "POST",
    route: "/board-items/:id/approval",
    async handle({ params, body, store }) {
      const decision = body?.decision;
      if (decision !== "approve" && decision !== "request-changes") {
        return validationError({
          decision: ["Use approve or request-changes."],
        });
      }
      return outcome(
        decideApproval(store, params.id, decision, body?.comment ?? null),
      );
    },
  }),

  // ── Reopen a completed item ──────────────────────────────────────────────
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.reopen",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "POST",
    route: "/board-items/:id/reopen",
    async handle({ params, store }) {
      return outcome(reopenItem(store, params.id));
    },
  }),

  // ── Status by bucket ─────────────────────────────────────────────────────
  defineMockOperation<{ id: string }, { bucket?: string }>({
    id: "wsFeed.items.setBucket",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "PUT",
    route: "/board-items/:id/status-bucket",
    async handle({ params, body, store }) {
      if (!isBucket(body?.bucket)) {
        return validationError({
          bucket: ["Use todo, doing, review or done."],
        });
      }
      return outcome(setItemBucket(store, params.id, body.bucket));
    },
  }),

  // ── Follow / unfollow ────────────────────────────────────────────────────
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.follow",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "PUT",
    route: "/board-items/:id/following",
    async handle({ params, store }) {
      return outcome(setFollowing(store, params.id, true));
    },
  }),
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.unfollow",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "DELETE",
    route: "/board-items/:id/following",
    async handle({ params, store }) {
      return outcome(setFollowing(store, params.id, false));
    },
  }),

  // ── Remind the person in charge ──────────────────────────────────────────
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.remind",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "POST",
    route: "/board-items/:id/remind",
    async handle({ params, store }) {
      return outcome(remindOwner(store, params.id));
    },
  }),

  // ── Bulk write ───────────────────────────────────────────────────────────
  defineMockOperation<
    { workspaceId: string },
    { itemIds?: string[]; action?: Record<string, unknown> }
  >({
    id: "wsFeed.myWork.bulk",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "POST",
    route: "/workspaces/:workspaceId/my-work/bulk",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const ids = body?.itemIds;
      const action = body?.action as BulkAction | undefined;
      if (!Array.isArray(ids) || ids.length === 0 || ids.length > 100) {
        return validationError({ itemIds: ["Give 1 to 100 item ids."] });
      }
      if (
        !action ||
        !(
          (action.type === "set-due" &&
            (action.dueAt === null || typeof action.dueAt === "string")) ||
          (action.type === "set-status" && isBucket(action.bucket)) ||
          action.type === "unfollow" ||
          (action.type === "assign" && typeof action.assigneeId === "string")
        )
      ) {
        return validationError({ action: ["The action is not valid."] });
      }
      return ok({ results: bulkWrite(store, ids, action) });
    },
  }),

  // ── Create an item assigned to the member ────────────────────────────────
  defineMockOperation<
    { workspaceId: string },
    { boardId?: string; name?: string }
  >({
    id: "wsFeed.myWork.createItem",
    contract: { kind: "gap", gapId: MY_WORK_EXT },
    method: "POST",
    route: "/workspaces/:workspaceId/my-work/items",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const name = body?.name?.trim() ?? "";
      if (!body?.boardId || name.length === 0 || name.length > 300) {
        return validationError({
          name: ["Give a name of 1 to 300 characters."],
        });
      }
      const made = createAssignedItem(store, body.boardId, name);
      return made === "not-found" ? notFound("Board not found") : created(made);
    },
  }),

  // ── Home feed ────────────────────────────────────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.home.feed",
    contract: { kind: "gap", gapId: HOME_FEED },
    method: "GET",
    route: "/workspaces/:workspaceId/home/feed",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(buildHomeFeed(store));
    },
  }),
  defineMockOperation<{ id: string }, { text?: string }>({
    id: "wsFeed.mentions.reply",
    contract: { kind: "gap", gapId: HOME_FEED },
    method: "POST",
    route: "/mentions/:id/reply",
    async handle({ params, body, store }) {
      const text = body?.text?.trim() ?? "";
      if (text.length === 0 || text.length > 4000) {
        return validationError({ text: ["Write 1 to 4000 characters."] });
      }
      return outcome(replyToMention(store, params.id, text));
    },
  }),

  // ── Inbox ────────────────────────────────────────────────────────────────
  defineMockOperation({
    id: "wsFeed.inbox.summary",
    contract: { kind: "gap", gapId: INBOX },
    method: "GET",
    route: "/inbox/summary",
    async handle({ store }) {
      return ok(buildInboxSummary(store));
    },
  }),
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.inbox.list",
    contract: { kind: "gap", gapId: INBOX },
    method: "GET",
    route: "/workspaces/:workspaceId/inbox",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(buildInbox(store, params.workspaceId));
    },
  }),
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.inbox.readAll",
    contract: { kind: "gap", gapId: INBOX },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/read-all",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markAllInboxRead(store);
      return noContent();
    },
  }),
  defineMockOperation<{ id: string }>({
    id: "wsFeed.inbox.handled",
    contract: { kind: "gap", gapId: INBOX },
    method: "POST",
    route: "/inbox/:id/handled",
    async handle({ params, store }) {
      return outcome(markInbox(store, params.id, { type: "handled" }));
    },
  }),
  defineMockOperation<{ id: string }, { until?: string }>({
    id: "wsFeed.inbox.snooze",
    contract: { kind: "gap", gapId: INBOX },
    method: "POST",
    route: "/inbox/:id/snooze",
    async handle({ params, body, store }) {
      const until = body?.until;
      if (typeof until !== "string" || Number.isNaN(Date.parse(until))) {
        return validationError({ until: ["Give a future ISO time."] });
      }
      return outcome(markInbox(store, params.id, { type: "snooze", until }));
    },
  }),
  defineMockOperation<{ id: string }>({
    id: "wsFeed.inbox.mute",
    contract: { kind: "gap", gapId: INBOX },
    method: "POST",
    route: "/inbox/:id/mute",
    async handle({ params, store }) {
      return outcome(markInbox(store, params.id, { type: "mute" }));
    },
  }),
  defineMockOperation<{ id: string }, { text?: string }>({
    id: "wsFeed.inbox.reply",
    contract: { kind: "gap", gapId: INBOX },
    method: "POST",
    route: "/inbox/:id/reply",
    async handle({ params, body, store }) {
      const text = body?.text?.trim() ?? "";
      if (text.length === 0 || text.length > 4000) {
        return validationError({ text: ["Write 1 to 4000 characters."] });
      }
      return outcome(markInbox(store, params.id, { type: "reply", text }));
    },
  }),
];
