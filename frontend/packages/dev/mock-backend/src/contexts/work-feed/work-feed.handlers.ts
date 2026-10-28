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
  listItemApprovals,
  requestItemApproval,
  buildMyWorkExtended,
  cancelApproval,
  decideApproval,
  markAllInboxRead,
  markInbox,
  remindOwner,
  reopenItem,
  setFollowing,
  type FeedWriteResult,
} from "../../state/mock-work-feed";
import { isoOfMs } from "../../state/clock";
import type { MockStore } from "../../state/mock-store";
import { commandSearch } from "../../state/mock-command-search";
import { spaceOverview } from "../../state/mock-space-overview";
import { spaceMembersExtra } from "../../state/mock-space-members";
import { securityExt, teamsExt } from "../../state/mock-teams-ext";
import { canReadBoard } from "../work-management/boards.handlers";

const MY_WORK_EXT = "CTR-GAP-WS-MYWORK-EXT" as const;

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

/** When the caller last reminded each item, per store (the 24 hour limit). */
const remindSentAt = new WeakMap<MockStore, Map<string, string>>();

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
  // ── Command palette search ────────────────────────────────────────────────
  defineMockOperation<{ workspaceId: string }>({
    id: "wsFeed.commandSearch",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Search.CommandSearch",
    },
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
    { workspaceId: string; requestId: string },
    { decision?: string; comment?: string | null }
  >({
    id: "wsFeed.approvals.decide",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Approvals.Decide",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/approvals/:requestId/decision",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const decision = body?.decision;
      if (decision !== "approve" && decision !== "reject") {
        return validationError({
          decision: ["The decision must be approve or reject."],
        });
      }
      return outcome(
        decideApproval(
          store,
          params.requestId,
          decision,
          body?.comment ?? null,
        ),
      );
    },
  }),

  defineMockOperation<{ requestId: string }, { expectedVersion?: number }>({
    id: "wsFeed.approvals.cancel",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Approvals.Cancel",
    },
    method: "POST",
    route: "/approvals/:requestId/cancel",
    async handle({ params, body, store }) {
      return outcome(
        cancelApproval(store, params.requestId, body?.expectedVersion),
      );
    },
  }),

  // ── Approvals asked about an item ────────────────────────────────────────
  defineMockOperation<{ id: string }>({
    id: "wsFeed.items.approvals",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Approvals.ListForItem",
    },
    method: "GET",
    route: "/board-items/:id/approval-requests",
    async handle({ params, store }) {
      const approvals = listItemApprovals(store, params.id);
      return approvals === "not-found" ? notFound() : ok(approvals);
    },
  }),
  defineMockOperation<
    { id: string },
    { approverUserId?: string; note?: string | null }
  >({
    id: "wsFeed.items.requestApproval",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Approvals.RequestForItem",
    },
    method: "POST",
    route: "/board-items/:id/approval-requests",
    async handle({ params, body, store }) {
      const approver = body?.approverUserId;
      if (typeof approver !== "string" || approver === "") {
        return validationError({
          approverUserId: ["Choose who should approve."],
        });
      }
      const result = requestItemApproval(
        store,
        params.id,
        approver,
        body?.note ?? null,
      );
      switch (result.kind) {
        case "ok":
          return ok({ id: result.id });
        case "not-found":
          return notFound();
        case "invalid":
          return validationError({ approverUserId: [result.message] });
        default:
          return conflict(
            "This person was already asked to approve this item.",
          );
      }
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
      const card = store.getCard(params.id);
      if (!card) return notFound();
      remindOwner(store, params.id);
      // Like the real command: everyone in charge but the caller, once per 24 hours.
      const me = store.getCurrentUser().id;
      const recipients = (card.assigneeUserIds ?? []).filter((id) => id !== me);
      if (recipients.length === 0)
        return {
          status: 400,
          body: {
            message: "Nobody else is in charge of this item.",
            code: "work.item.remind-no-recipients",
          },
        };
      const now = store.getClock().isoNow();
      const sent = remindSentAt.get(store) ?? new Map<string, string>();
      remindSentAt.set(store, sent);
      const last = sent.get(params.id);
      const next = (iso: string) =>
        isoOfMs(Date.parse(iso) + 24 * 3_600_000);
      if (last && Date.parse(next(last)) > Date.parse(now))
        return {
          status: 409,
          body: {
            message: "A reminder was sent in the last 24 hours.",
            code: "work.item.remind-too-soon",
            nextRemindAt: next(last),
          },
        };
      sent.set(params.id, now);
      return ok({ recipients: recipients.length, nextRemindAt: next(now) });
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
    async handle({ params, query, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const state = query["state"]?.toLowerCase();
      if (
        state !== undefined &&
        !["inbox", "done", "snoozed"].includes(state)
      ) {
        return validationError({
          state: ["The state must be inbox, done or snoozed."],
        });
      }
      // The real producer lists notifications: a mention is the one kind it has.
      const inbox = buildInbox(
        store,
        params.workspaceId,
        (state as "inbox" | "done" | "snoozed" | undefined) ?? "inbox",
      );
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
            needsReply: entry.need === "reply",
            muted: entry.muted,
            snoozedUntil: entry.snoozedUntil,
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
  defineMockOperation<{ workspaceId: string; id: string }>({
    id: "wsFeed.inbox.done",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.MarkInboxEntryDone",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/done",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markInbox(store, params.id, { type: "done" });
      return noContent();
    },
  }),
  defineMockOperation<{ workspaceId: string; id: string }>({
    id: "wsFeed.inbox.reopen",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.ReopenInboxEntry",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/reopen",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markInbox(store, params.id, { type: "reopen" });
      return noContent();
    },
  }),
  defineMockOperation<{ workspaceId: string; id: string }, { until?: string }>({
    id: "wsFeed.inbox.snooze",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.SnoozeInboxEntry",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/snooze",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const until = Date.parse(body?.until ?? "");
      const now = Date.parse(store.getClock().isoNow());
      if (Number.isNaN(until) || until <= now) {
        return validationError({ until: ["A snooze must end in the future."] });
      }
      if (until > now + 365 * 24 * 60 * 60 * 1000) {
        return validationError({
          until: ["A snooze can last at most 365 days."],
        });
      }
      markInbox(store, params.id, {
        type: "snooze",
        until: store.getClock().offsetSeconds((until - now) / 1000),
      });
      return noContent();
    },
  }),
  defineMockOperation<{ workspaceId: string; id: string }>({
    id: "wsFeed.inbox.mute",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.MuteInboxThread",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/mute",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markInbox(store, params.id, { type: "mute", muted: true });
      return noContent();
    },
  }),
  defineMockOperation<{ workspaceId: string; id: string }>({
    id: "wsFeed.inbox.unmute",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Notifications.UnmuteInboxThread",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/inbox/:id/unmute",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      markInbox(store, params.id, { type: "mute", muted: false });
      return noContent();
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
