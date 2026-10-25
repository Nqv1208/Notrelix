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
  isFollowing,
  listChildItems,
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
import type { MockStore } from "../../state/mock-store";
import { commandSearch } from "../../state/mock-command-search";
import { spaceOverview } from "../../state/mock-space-overview";
import { spaceMembersExtra } from "../../state/mock-space-members";
import { securityExt, teamsExt } from "../../state/mock-teams-ext";
import { canReadBoard } from "../work-management/boards.handlers";

const MY_WORK_EXT = "CTR-GAP-WS-MYWORK-EXT" as const;
const HOME_FEED = "CTR-GAP-WS-HOME-FEED" as const;

const PRIORITY_LABELS = {
  urgent: "Urgent",
  high: "High",
  medium: "Medium",
  low: "Low",
} as const;

/**
 * The producer states a Priority as the option the Item holds (id and label);
 * the page maps the label. The mock keeps its own priority keys, so each one
 * is stated here as a stable option.
 */
function withPriorityOption(dto: ReturnType<typeof buildMyWorkExtended>) {
  const convert = (item: (typeof dto.items)[number]) => ({
    ...item,
    priority: item.priority
      ? {
          optionId: `00000000-0000-4000-8000-00000000000${Object.keys(PRIORITY_LABELS).indexOf(item.priority) + 1}`,
          label: PRIORITY_LABELS[item.priority],
        }
      : null,
  });
  return {
    ...dto,
    items: dto.items.map(convert),
    done: dto.done.map(convert),
  };
}

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

/**
 * The people who follow an item: the member when
 * they follow it, the assignees, then the others who chose to.
 */
function followersOf(store: MockStore, cardId: string, memberFollows: boolean) {
  const card = store.getCard(cardId);
  const ids = [
    ...(memberFollows ? [store.getCurrentUser().id] : []),
    ...(card?.assigneeUserIds ?? []),
    ...store.getFollowerIds(cardId),
  ];
  return [...new Set(ids)].flatMap((userId) => {
    const user = store.getUser(userId);
    return user ? [{ userId, name: user.name }] : [];
  });
}

export const workFeedOperations = [
  // ── Command palette search (CTR-GAP-WS-COMMAND-SEARCH) ───────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.commandSearch",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-COMMAND-SEARCH" },
    method: "GET",
    route: "/workspaces/:workspaceId/command-search",
    async handle({ params, query, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(
        commandSearch(store, params.workspaceId, query["q"] ?? "", (id) =>
          canReadBoard(id, store),
        ),
      );
    },
  }),

  // ── Department page summary (CTR-GAP-WS-SPACE-OVERVIEW) ──────────────────
  defineMockOperation<{ workspaceId: string; spaceId: string }>({
    id: "wsFeed.spaceOverview",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-SPACE-OVERVIEW" },
    method: "GET",
    route: "/workspaces/:workspaceId/spaces/:spaceId/overview",
    async handle({ params, query, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const overview = spaceOverview(
        store,
        params.workspaceId,
        params.spaceId,
        {
          canReadBoard: (id) => canReadBoard(id, store),
          asMember: query["as"] === "member",
        },
      );
      return overview ? ok(overview) : notFound();
    },
  }),

  // ── Department members page extras (CTR-GAP-WS-MEMBERS-EXT) ──────────────
  defineMockOperation<{ workspaceId: string; spaceId: string }>({
    id: "wsFeed.spaceMembersExtra",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-MEMBERS-EXT" },
    method: "GET",
    route: "/workspaces/:workspaceId/spaces/:spaceId/members-extra",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const extra = spaceMembersExtra(
        store,
        params.workspaceId,
        params.spaceId,
        (id) => canReadBoard(id, store),
      );
      return extra ? ok(extra) : notFound();
    },
  }),

  // ── Teams pages extras (CTR-GAP-WS-TEAMS-EXT) ────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.teamsExt",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-TEAMS-EXT" },
    method: "GET",
    route: "/workspaces/:workspaceId/teams-ext",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(teamsExt(store, params.workspaceId));
    },
  }),

  // ── Sharing and security page extras (CTR-GAP-WS-SECURITY-EXT) ───────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.securityExt",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-SECURITY-EXT" },
    method: "GET",
    route: "/workspaces/:workspaceId/security-ext",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(securityExt(store, params.workspaceId));
    },
  }),

  // ── My work: extended read ───────────────────────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.myWork.extended",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.GetMyWorkExtended",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/my-work/extended",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(
        withPriorityOption(buildMyWorkExtended(store, params.workspaceId)),
      );
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
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Reopen",
    },
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
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Watchers.FollowBoardItem",
    },
    method: "PUT",
    route: "/board-items/:id/following",
    async handle({ params, store }) {
      return outcome(setFollowing(store, params.id, true));
    },
  }),
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.unfollow",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Watchers.UnfollowBoardItem",
    },
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

  // ── Item sheet: child items and following ────────────────────────────────
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.childItems",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.ListChildren",
    },
    method: "GET",
    route: "/board-items/:id/child-items",
    async handle({ params, store }) {
      const items = listChildItems(store, params.id);
      return items === "not-found" ? notFound() : ok({ items });
    },
  }),
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.followingState",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Watchers.GetBoardItemFollowing",
    },
    method: "GET",
    route: "/board-items/:id/following",
    async handle({ params, store }) {
      const following = isFollowing(store, params.id);
      if (following === "not-found") return notFound();
      return ok({
        following,
        followers: followersOf(store, params.id, following),
      });
    },
  }),

  // ── Home feed ────────────────────────────────────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.home.feed",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.GetHomeFeed",
    },
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
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.GetInboxSummary",
    },
    method: "GET",
    route: "/inbox/summary",
    async handle({ store }) {
      return ok(buildInboxSummary(store));
    },
  }),
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.inbox.list",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.GetWorkspaceInbox",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/inbox",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      // The real producer lists notifications: a mention is the one kind it has.
      const inbox = buildInbox(store, params.workspaceId);
      return ok({
        items: inbox.items
          .filter((entry) => entry.type === "mention")
          .map((entry) => ({
            id: entry.id,
            type: "mention.created",
            createdAt: entry.createdAt,
            unread: entry.unread,
            actor: entry.actor,
            excerpt: entry.excerpt,
            target: {
              kind: "item",
              id: entry.target.id,
              boardId: entry.target.boardId,
              key: entry.target.key,
              title: entry.target.title,
            },
          })),
        nextCursor: null,
      });
    },
  }),
  defineMockOperation<{ workspaceId: string; id: string }>({
    id: "wsFeed.inbox.read",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.MarkInboxEntryRead",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/read",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return outcome(markInbox(store, params.id, { type: "handled" }));
    },
  }),
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.inbox.readAll",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.MarkWorkspaceInboxRead",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/read-all",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markAllInboxRead(store);
      return ok({ marked: 0 });
    },
  }),
];
