/**
 * The member's work feed (Home, My work, Inbox).
 *
 * The My work read (`WorkManagement.BoardItems.GetMyWorkExtended`), the Home
 * feed (`WorkManagement.BoardItems.GetHomeFeed`) and the inbox summary
 * (`Collaboration.Notifications.GetInboxSummary`) are bound to the generated
 * contract and mapped below into the DTO types the pages draw. Creating an
 * item, changing status or due date, assigning and unfollowing are per-item
 * writes on the Work Management contract (`work-feed-item-writes.ts`). The
 * remaining reads and writes here are still registered gaps
 * (`CTR-GAP-WS-MYWORK-EXT`, `CTR-GAP-WS-HOME-FEED`, `CTR-GAP-WS-INBOX` in
 * `tooling/contracts/enabled-consumer-surface.ts`) served by the mock backend.
 * The DTO types below are the frontend's own and the single place the shapes
 * live.
 *
 * TRANSITION: like `work-management-bridge.ts`, these adapters stay thin
 * (producer calls and cache effects, no business rules) and move to the owning
 * product packages once the producer contract exists.
 */
import { useCallback, useMemo } from "react";
import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import {
  accountQueryKey,
  useAccountId,
  workspaceQueryKey,
} from "@notrelix/query";
import type { OperationResponse } from "@notrelix/contracts";
import { useAppRuntime } from "@notrelix/runtime-web";
import { priorityFromOptionName } from "@notrelix/work-management-core";
import {
  seenItemVersion,
  wmQueryKeys,
} from "@notrelix/work-management-state";
import { useCurrentUser } from "@notrelix/features-auth";
import { myWorkQueryKey } from "./work-management-bridge";
import {
  bulkEach,
  createAssignedItem,
  setItemBucket,
  type BulkAction,
  type BulkResult,
  type WorkBucket,
} from "./work-feed-item-writes";

// ─── DTOs (frontend-owned; see the BE requirement log for the contract) ──────

export type WorkBucketDto = WorkBucket;
export type WorkPriorityDto = "urgent" | "high" | "medium" | "low";

export interface PersonDto {
  readonly id: string;
  readonly name: string;
}

export interface StatusDto {
  readonly optionId: string;
  readonly label: string;
  readonly bucket: WorkBucketDto;
}

export type MyWorkRelationDto = "assigned" | "following" | "approver";

export interface MyWorkItemExtDto {
  readonly id: string;
  readonly boardId: string;
  readonly groupId: string;
  readonly key: string | null;
  readonly name: string;
  readonly dueAt: string | null;
  readonly startedAt: string | null;
  readonly updatedAt: string | null;
  readonly version: number;
  readonly status: StatusDto | null;
  readonly priority: WorkPriorityDto | null;
  readonly relations: readonly MyWorkRelationDto[];
  readonly assignee: PersonDto | null;
  readonly approval: {
    readonly requestId: string;
    readonly requestedBy: PersonDto;
    readonly requestedAt: string;
  } | null;
  readonly completedAt: string | null;
}

export interface MyWorkWaitingDto {
  readonly itemId: string;
  readonly boardId: string;
  readonly key: string | null;
  readonly title: string;
  readonly owner: PersonDto;
  readonly dueAt: string | null;
  readonly note: string;
  readonly lastRemindedAt: string | null;
}

export interface MyWorkExtendedDto {
  readonly items: readonly MyWorkItemExtDto[];
  readonly done: readonly MyWorkItemExtDto[];
  readonly waiting: readonly MyWorkWaitingDto[];
  readonly departments: Readonly<Record<string, string | null>>;
}

export interface HomeReplyDto {
  readonly id: string;
  readonly kind: "approval" | "mention";
  readonly actor: PersonDto;
  readonly createdAt: string;
  readonly text: string;
  readonly target: {
    readonly kind: "item" | "doc";
    readonly id: string;
    readonly boardId: string | null;
    readonly key: string | null;
    readonly title: string | null;
  };
}

export interface CalendarEventDto {
  readonly id: string;
  readonly title: string;
  readonly startsAt: string;
  readonly endsAt: string;
  readonly place: string | null;
  readonly attendeeCount: number | null;
}

export interface HomeFeedDto {
  readonly replies: readonly HomeReplyDto[];
  readonly events: readonly CalendarEventDto[];
  readonly summary: {
    readonly approvalsPending: number;
    readonly mentionsUnread: number;
  };
}

export type InboxTypeDto =
  | "approval_requested"
  | "approval_approved"
  | "approval_rejected"
  | "mention"
  | "overdue"
  | "assigned"
  | "changes_bundle"
  | "board_invited"
  | "followed_status"
  | "followed_comment"
  | "followed_assignee"
  | "automation"
  | "due_tomorrow"
  | "system";

export interface InboxTargetDto {
  readonly kind: "item" | "doc" | "board";
  readonly id: string | null;
  readonly key: string | null;
  readonly title: string;
  readonly boardId: string | null;
  readonly boardName: string | null;
  readonly departmentId: string | null;
  readonly owner: PersonDto | null;
}

export interface InboxBundleChildDto {
  readonly id: string;
  readonly createdAt: string;
  readonly actor: PersonDto;
  readonly kind: "status" | "comment" | "due";
  readonly itemKey: string;
  readonly itemTitle: string;
  readonly detail: string;
}

export interface InboxEntryDto {
  readonly id: string;
  readonly createdAt: string;
  readonly unread: boolean;
  readonly type: InboxTypeDto;
  readonly actor: PersonDto | null;
  readonly need: "approve" | "reply" | "overdue" | null;
  /** The approval request an "approval requested" entry is about, and where it stands. */
  readonly approval: {
    readonly requestId: string;
    readonly status: "pending" | "approved" | "rejected" | "cancelled";
  } | null;
  readonly following: boolean;
  readonly muted: boolean;
  /** The member marked it done; only the Done list holds such entries. */
  readonly done: boolean;
  /** When a snoozed entry returns, if it is snoozed. */
  readonly snoozedUntil: string | null;
  readonly target: InboxTargetDto;
  readonly excerpt: string | null;
  readonly dueAt: string | null;
  readonly permission: "view" | "comment" | "edit" | null;
  readonly bundle: {
    readonly actors: readonly PersonDto[];
    readonly moreActors: number;
    readonly statusChanges: number;
    readonly comments: number;
    readonly dueChanges: number;
    readonly children: readonly InboxBundleChildDto[];
  } | null;
  readonly detail: {
    readonly approval: {
      readonly soleApprover: boolean;
      readonly summary: string;
      readonly changes: readonly {
        readonly label: string;
        readonly value: string;
        readonly changed?: string;
      }[];
    } | null;
    readonly item: {
      readonly status: StatusDto | null;
      readonly priority: WorkPriorityDto | null;
      readonly dueAt: string | null;
      readonly commentCount: number;
    } | null;
    readonly latestComment: {
      readonly author: PersonDto;
      readonly createdAt: string;
      readonly text: string;
    } | null;
  } | null;
}

export interface InboxDto {
  readonly workspace: { readonly id: string; readonly name: string };
  readonly items: readonly InboxEntryDto[];
  readonly counts: {
    readonly need: number;
    readonly mention: number;
    readonly assigned: number;
    readonly follow: number;
    readonly all: number;
    readonly unread: number;
  };
}

export interface InboxSummaryDto {
  readonly perWorkspace: readonly {
    readonly workspaceId: string;
    readonly unread: number;
  }[];
  readonly total: number;
}

export type BulkActionDto = BulkAction;
export type BulkResultDto = BulkResult;

// ─── Generated contract → page DTOs ─────────────────────────────────────────

type MyWorkExtendedResponse = OperationResponse<
  "WorkManagement.BoardItems.GetMyWorkExtended",
  200
>;
type HomeFeedResponse = OperationResponse<
  "WorkManagement.BoardItems.GetHomeFeed",
  200
>;
type InboxSummaryResponse = OperationResponse<
  "Collaboration.Notifications.GetInboxSummary",
  200
>;

const BUCKETS: ReadonlySet<string> = new Set([
  "todo",
  "doing",
  "review",
  "done",
]);

function toPerson(
  raw: { id?: string; name?: string | null } | null | undefined,
): PersonDto | null {
  return raw?.id ? { id: raw.id, name: raw.name ?? "" } : null;
}

function toExtItem(
  raw: NonNullable<MyWorkExtendedResponse["items"]>[number],
): MyWorkItemExtDto | null {
  if (!raw.id || !raw.boardId) return null;
  const status = raw.status;
  const priority = raw.priority
    ? (priorityFromOptionName(raw.priority.label) ?? null)
    : null;
  const requestedBy = toPerson(raw.approval?.requestedBy);
  return {
    id: raw.id,
    boardId: raw.boardId,
    groupId: raw.groupId ?? "",
    key: raw.key ?? null,
    name: raw.name ?? "",
    dueAt: raw.dueAt ?? null,
    startedAt: raw.startedAt ?? null,
    updatedAt: raw.updatedAt ?? null,
    version: raw.version ?? 0,
    status:
      status?.optionId && status.bucket && BUCKETS.has(status.bucket)
        ? {
            optionId: status.optionId,
            label: status.label ?? "",
            bucket: status.bucket as WorkBucketDto,
          }
        : null,
    priority,
    relations: (raw.relations ?? []).filter(
      (r): r is MyWorkRelationDto =>
        r === "assigned" || r === "following" || r === "approver",
    ),
    assignee: toPerson(raw.assignee),
    approval:
      requestedBy && raw.approval?.requestId && raw.approval.requestedAt
        ? {
            requestId: raw.approval.requestId,
            requestedBy,
            requestedAt: raw.approval.requestedAt,
          }
        : null,
    completedAt: raw.completedAt ?? null,
  };
}

export function toMyWorkExtended(
  dto: MyWorkExtendedResponse | null,
): MyWorkExtendedDto {
  const items = (list: MyWorkExtendedResponse["items"]) =>
    (list ?? []).flatMap((raw) => toExtItem(raw) ?? []);
  const departments: Record<string, string | null> = {};
  for (const [boardId, departmentId] of Object.entries(
    dto?.departments ?? {},
  )) {
    departments[boardId] = departmentId;
  }
  return {
    items: items(dto?.items),
    done: items(dto?.done),
    waiting: (dto?.waiting ?? []).flatMap((raw) => {
      const owner = toPerson(raw.owner);
      return raw.itemId && raw.boardId && owner
        ? [
            {
              itemId: raw.itemId,
              boardId: raw.boardId,
              key: raw.key ?? null,
              title: raw.title ?? "",
              owner,
              dueAt: raw.dueAt ?? null,
              // The real waiting-on read carries no note yet.
              note: "",
              lastRemindedAt: raw.lastRemindedAt ?? null,
            },
          ]
        : [];
    }),
    departments,
  };
}

export function toHomeFeed(dto: HomeFeedResponse | null): HomeFeedDto {
  return {
    replies: (dto?.replies ?? []).flatMap((raw) => {
      const actor = toPerson(raw.actor);
      const target = raw.target;
      if (!raw.id || !actor || !raw.createdAt || !target?.id) return [];
      const reply: HomeReplyDto = {
        id: raw.id,
        kind: raw.kind === "approval" ? "approval" : "mention",
        actor,
        createdAt: raw.createdAt,
        text: raw.text ?? "",
        target: {
          kind: target.kind === "doc" ? "doc" : "item",
          id: target.id,
          boardId: target.boardId ?? null,
          key: target.key ?? null,
          title: target.title ?? null,
        },
      };
      return [reply];
    }),
    events: (dto?.events ?? []).flatMap((raw) =>
      raw.id && raw.startsAt && raw.endsAt
        ? [
            {
              id: raw.id,
              title: raw.title ?? "",
              startsAt: raw.startsAt,
              endsAt: raw.endsAt,
              place: raw.place ?? null,
              attendeeCount: raw.attendeeCount ?? null,
            },
          ]
        : [],
    ),
    summary: {
      approvalsPending: dto?.summary?.approvalsPending ?? 0,
      mentionsUnread: dto?.summary?.mentionsUnread ?? 0,
    },
  };
}

export function toInboxSummary(
  dto: InboxSummaryResponse | null,
): InboxSummaryDto {
  return {
    perWorkspace: (dto?.perWorkspace ?? []).flatMap((raw) =>
      raw.workspaceId
        ? [{ workspaceId: raw.workspaceId, unread: raw.unread ?? 0 }]
        : [],
    ),
    total: dto?.total ?? 0,
  };
}

// ─── Paths ───────────────────────────────────────────────────────────────────

const path = {
  myWorkExtended: (workspaceId: string) =>
    `/workspaces/${workspaceId}/my-work/extended`,
  approvalDecision: (workspaceId: string, requestId: string) =>
    `/workspaces/${workspaceId}/approvals/${requestId}/decision`,
  reopen: (itemId: string) => `/board-items/${itemId}/reopen`,
  remind: (itemId: string) => `/board-items/${itemId}/remind`,
  homeFeed: (workspaceId: string) => `/workspaces/${workspaceId}/home/feed`,
  inbox: (workspaceId: string) => `/workspaces/${workspaceId}/inbox`,
  inboxReadAll: (workspaceId: string) =>
    `/workspaces/${workspaceId}/inbox/read-all`,
  inboxEntryRead: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/read`,
  inboxEntryDone: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/done`,
  inboxEntryReopen: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/reopen`,
  inboxEntrySnooze: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/snooze`,
  inboxEntryMute: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/mute`,
  inboxEntryUnmute: (workspaceId: string, id: string) =>
    `/workspaces/${workspaceId}/inbox/${id}/unmute`,
  itemComments: (itemId: string) => `/board-items/${itemId}/comments`,
  pageComments: (pageId: string) => `/pages/${pageId}/comments`,
};

export const myWorkExtendedQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "my-work-extended");
export const homeFeedQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "workspace-feed", "home");
export const inboxQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "workspace-feed", "inbox");

/** Which list of the inbox the page shows: the default one or the done entries. */
export type InboxListView = "inbox" | "done";

const QUIET = { skipGlobalErrorToast: true } as const;

// ─── Reads ───────────────────────────────────────────────────────────────────

export function useMyWorkExtended(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: myWorkExtendedQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    queryFn: async () =>
      toMyWorkExtended(
        await client.api.get<MyWorkExtendedResponse | null>(
          path.myWorkExtended(workspaceId),
        ),
      ),
  });
}

export function useHomeFeed(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: homeFeedQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    queryFn: async () =>
      toHomeFeed(
        await client.api.get<HomeFeedResponse | null>(
          path.homeFeed(workspaceId),
        ),
      ),
  });
}

/** Unread inbox entries per workspace and in total (rail and sidebar badges). */
export function useInboxSummary() {
  const { api: client } = useAppRuntime();
  const accountId = useAccountId();
  return useQuery({
    queryKey: accountQueryKey(accountId, "workspace-feed", "inbox-summary"),
    enabled: Boolean(accountId),
    staleTime: 15_000,
    queryFn: async () =>
      toInboxSummary(
        await client.api.get<InboxSummaryResponse | null>("/inbox/summary"),
      ),
  });
}

type WorkspaceInboxResponse =
  OperationResponse<"Collaboration.Notifications.GetWorkspaceInbox">;

/** The most notifications the Inbox page loads; the producer pages beyond it. */
const INBOX_PAGE_LIMIT = 30;

/** The producer's notification types the Inbox page draws, and how. */
const INBOX_TYPES: Readonly<Record<string, InboxTypeDto>> = {
  "mention.created": "mention",
  "work.item.assigned": "assigned",
  "work.item.assignee-added": "followed_assignee",
  "work.item.status-changed": "followed_status",
  "work.item.commented": "followed_comment",
  "work.item.automation": "automation",
  "work.item.due-tomorrow": "due_tomorrow",
  "work.item.overdue": "overdue",
  "approval.requested": "approval_requested",
  "approval.approved": "approval_approved",
  "approval.rejected": "approval_rejected",
};

const APPROVAL_STATUSES = {
  pending: "pending",
  approved: "approved",
  rejected: "rejected",
  cancelled: "cancelled",
} as const;

/**
 * The producer's notifications as the Inbox page lists them. A type this
 * page does not know is drawn as a generic system row, never dropped. A
 * mention the member has not answered carries the "reply" need; the bundle and
 * detail parts of an entry have no source and stay empty.
 */
export function toInboxDto(
  workspaceId: string,
  dto: WorkspaceInboxResponse | null,
  view: InboxListView = "inbox",
): InboxDto {
  const items = (dto?.items ?? []).map((entry): InboxEntryDto => {
    const type = INBOX_TYPES[entry.type ?? ""] ?? "system";
    const status =
      APPROVAL_STATUSES[
        (
          entry.approval?.status ?? ""
        ).toLowerCase() as keyof typeof APPROVAL_STATUSES
      ];
    const approval =
      entry.approval?.requestId && status
        ? { requestId: entry.approval.requestId, status }
        : null;
    return {
      id: entry.id ?? "",
      createdAt: entry.createdAt ?? "",
      unread: entry.unread === true,
      type,
      actor: entry.actor?.id
        ? { id: entry.actor.id, name: entry.actor.name ?? "" }
        : null,
      need:
        entry.needsReply === true
          ? "reply"
          : type === "approval_requested" && approval?.status === "pending"
            ? "approve"
            : type === "overdue"
              ? "overdue"
              : null,
      approval,
      following: type.startsWith("followed_"),
      muted: entry.muted === true,
      done: view === "done",
      snoozedUntil: entry.snoozedUntil ?? null,
      target: {
        kind: entry.target?.kind === "doc" ? "doc" : "item",
        id: entry.target?.id ?? null,
        key: entry.target?.key ?? null,
        title: entry.target?.title ?? "",
        boardId: entry.target?.boardId ?? null,
        boardName: null,
        departmentId: null,
        owner: null,
      },
      excerpt: entry.excerpt ?? null,
      dueAt: null,
      permission: null,
      bundle: null,
      detail: null,
    };
  });
  const unread = items.filter((entry) => entry.unread).length;
  return {
    workspace: { id: workspaceId, name: "" },
    items,
    counts: {
      need: items.filter((entry) => entry.need !== null).length,
      mention: items.filter((entry) => entry.type === "mention").length,
      assigned: items.filter((entry) => entry.type === "assigned").length,
      follow: items.filter((entry) => entry.following).length,
      all: items.length,
      unread,
    },
  };
}

/** The pages of the inbox loaded so far, newest first, as one list. */
export function useWorkspaceInbox(
  workspaceId: string,
  view: InboxListView = "inbox",
) {
  const { api: client } = useAppRuntime();
  const query = useInfiniteQuery({
    queryKey: [...inboxQueryKey(workspaceId), view] as const,
    enabled: Boolean(workspaceId),
    staleTime: 15_000,
    initialPageParam: "",
    queryFn: async ({ pageParam }) =>
      client.api.get<WorkspaceInboxResponse | null>(
        `${path.inbox(workspaceId)}?limit=${INBOX_PAGE_LIMIT}${
          view === "done" ? "&state=done" : ""
        }${pageParam ? `&cursor=${encodeURIComponent(pageParam)}` : ""}`,
      ),
    getNextPageParam: (last) => last?.nextCursor || undefined,
  });
  const data = useMemo(
    () =>
      query.data
        ? toInboxDto(
            workspaceId,
            {
              items: query.data.pages.flatMap((page) => page?.items ?? []),
            },
            view,
          )
        : undefined,
    [query.data, view, workspaceId],
  );
  return {
    data,
    isPending: query.isPending,
    isError: query.isError,
    isSuccess: query.isSuccess,
    status: query.status,
    error: query.error,
    refetch: query.refetch,
    hasNextPage: query.hasNextPage,
    isFetchingNextPage: query.isFetchingNextPage,
    fetchNextPage: query.fetchNextPage,
  };
}

// ─── Writes ──────────────────────────────────────────────────────────────────

function invalidateFeed(
  queryClient: QueryClient,
  accountId: string,
  workspaceId: string,
  boardIds: readonly string[] = [],
) {
  void queryClient.invalidateQueries({
    queryKey: myWorkExtendedQueryKey(workspaceId),
  });
  void queryClient.invalidateQueries({ queryKey: myWorkQueryKey(workspaceId) });
  void queryClient.invalidateQueries({
    queryKey: homeFeedQueryKey(workspaceId),
  });
  void queryClient.invalidateQueries({ queryKey: inboxQueryKey(workspaceId) });
  void queryClient.invalidateQueries({
    queryKey: accountQueryKey(accountId, "workspace-feed"),
  });
  for (const boardId of new Set(boardIds)) {
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
  }
}

/**
 * Refreshes My work, Home, the Inbox and the Boards named, for a write the
 * work feed did not make itself (the board's own "Add item" form).
 */
export function useRefreshWorkFeed(workspaceId: string) {
  const queryClient = useQueryClient();
  const accountId = useAccountId();
  return useCallback(
    (boardIds: readonly string[] = []) =>
      invalidateFeed(queryClient, accountId, workspaceId, boardIds),
    [queryClient, accountId, workspaceId],
  );
}

/** One idempotency key per user-initiated attempt. */
function attemptKey(): string {
  return crypto.randomUUID();
}

export interface WorkFeedWrites {
  readonly decideApproval: (input: {
    requestId: string;
    boardId: string;
    decision: "approve" | "reject";
    comment?: string;
  }) => Promise<void>;
  readonly reopen: (input: {
    itemId: string;
    boardId: string;
  }) => Promise<void>;
  readonly setStatus: (input: {
    itemId: string;
    boardId: string;
    bucket: WorkBucketDto;
  }) => Promise<void>;
  readonly remind: (itemId: string) => Promise<void>;
  readonly bulk: (input: {
    itemIds: readonly string[];
    boardIds: readonly string[];
    action: BulkActionDto;
  }) => Promise<BulkResultDto>;
  readonly createItem: (input: {
    boardId: string;
    name: string;
    descriptionMd?: string;
    dueDay?: string;
  }) => Promise<{ id: string; key: string | null }>;
  /**
   * Answers a mention shown on Home: the reply is the member's comment on the
   * thread they were mentioned in (the existing item or page comment write),
   * then the mention's notification is read, which takes it off Home.
   */
  readonly replyToMention: (input: {
    notificationId: string;
    target: { readonly kind: "item" | "doc"; readonly id: string };
    text: string;
  }) => Promise<void>;
  readonly inbox: {
    readonly markRead: (id: string) => Promise<void>;
    readonly readAll: () => Promise<void>;
    readonly done: (id: string) => Promise<void>;
    readonly reopen: (id: string) => Promise<void>;
    readonly snooze: (id: string, until: string) => Promise<void>;
    readonly mute: (id: string, muted: boolean) => Promise<void>;
    /** Posts a comment on the item the entry is about (the existing comment write). */
    readonly reply: (itemId: string, text: string) => Promise<void>;
    /** Posts a comment on the page the entry is about (the existing page comment write). */
    readonly replyOnPage: (pageId: string, text: string) => Promise<void>;
  };
}

/**
 * The version of an item as the feed showed it: the My work read's row, else the
 * Board read's or the item read's (the caches the screens render from). Throws
 * when none holds one: a write never runs on a fresh or guessed version.
 */
function feedItemVersion(
  queryClient: QueryClient,
  workspaceId: string,
  itemId: string,
  boardId: string | null,
): number {
  const feed = queryClient.getQueryData<MyWorkExtendedDto>(
    myWorkExtendedQueryKey(workspaceId),
  );
  const row = [...(feed?.items ?? []), ...(feed?.done ?? [])].find(
    (candidate) => candidate.id === itemId,
  );
  const version =
    row?.version ||
    (boardId
      ? seenItemVersion(queryClient, { workspaceId, boardId, cardId: itemId })
      : undefined);
  if (!version || version <= 0)
    throw new Error(`The version of item ${itemId} is unknown; reload first.`);
  return version;
}

/**
 * Every write of the work feed. Not optimistic: the producer answers, then the
 * reads of the three pages and the Board cache refetch (one source of truth).
 */
export function useWorkFeedWrites(workspaceId: string): WorkFeedWrites {
  const { api: client } = useAppRuntime();
  const queryClient = useQueryClient();
  const accountId = useAccountId();
  const userId = useCurrentUser()?.id ?? null;
  const api = client.api;

  const run = useMutation({
    mutationFn: async (job: {
      send: () => Promise<unknown>;
      boardIds?: readonly string[];
    }) => ({ value: await job.send(), boardIds: job.boardIds ?? [] }),
    onSettled: (_data, _error, job) =>
      invalidateFeed(queryClient, accountId, workspaceId, job.boardIds ?? []),
  });
  const perform = useCallback(
    async <T>(send: () => Promise<T>, boardIds: readonly string[] = []) =>
      (await run.mutateAsync({ send, boardIds })).value as T,
    [run],
  );

  return useMemo<WorkFeedWrites>(
    () => ({
      decideApproval: ({ requestId, boardId, decision, comment }) =>
        perform(
          () =>
            api.post<void>(
              path.approvalDecision(workspaceId, requestId),
              { decision, comment: comment ?? null },
              { idempotencyKey: attemptKey(), ...QUIET },
            ),
          [boardId],
        ),
      reopen: ({ itemId, boardId }) =>
        perform(
          () =>
            api.post<void>(
              `${path.reopen(itemId)}?expectedVersion=${feedItemVersion(queryClient, workspaceId, itemId, boardId)}`,
              {},
              {
                idempotencyKey: attemptKey(),
                ...QUIET,
              },
            ),
          [boardId],
        ),
      setStatus: ({ itemId, boardId, bucket }) =>
        perform(
          () =>
            setItemBucket(api, {
              itemId,
              boardId,
              bucket,
              versionOf: (id) =>
                feedItemVersion(queryClient, workspaceId, id, boardId),
            }),
          [boardId],
        ),
      remind: (itemId) =>
        perform(() =>
          api.post<void>(
            path.remind(itemId),
            {},
            {
              idempotencyKey: attemptKey(),
              ...QUIET,
            },
          ),
        ),
      bulk: ({ itemIds, boardIds, action }) =>
        perform(() => {
          // The items and their Boards are parallel lists.
          const boardOf = new Map(itemIds.map((id, i) => [id, boardIds[i]]));
          return bulkEach(api, {
            itemIds,
            boardIdOf: (itemId) => boardOf.get(itemId) ?? null,
            action,
            versionOf: (id) =>
              feedItemVersion(
                queryClient,
                workspaceId,
                id,
                boardOf.get(id) ?? null,
              ),
          });
        }, boardIds),
      createItem: ({ boardId, name, descriptionMd, dueDay }) =>
        perform(() => {
          if (!userId) throw new Error("Not signed in.");
          return createAssignedItem(api, {
            boardId,
            name,
            userId,
            descriptionMd,
            dueDay,
          });
        }, [boardId]),
      replyToMention: ({ notificationId, target, text }) =>
        perform(async () => {
          await api.post<string>(
            target.kind === "item"
              ? path.itemComments(target.id)
              : path.pageComments(target.id),
            target.kind === "item"
              ? { contentMd: text, parentCommentId: null }
              : { contentMd: text },
            { idempotencyKey: attemptKey(), ...QUIET },
          );
          await api.post<void>(
            path.inboxEntryRead(workspaceId, notificationId),
            {},
            QUIET,
          );
        }),
      inbox: {
        markRead: (id) =>
          perform(() =>
            api.post<void>(path.inboxEntryRead(workspaceId, id), {}, QUIET),
          ),
        readAll: () =>
          perform(() =>
            api.post<void>(path.inboxReadAll(workspaceId), {}, QUIET),
          ),
        done: (id) =>
          perform(() =>
            api.post<void>(path.inboxEntryDone(workspaceId, id), {}, QUIET),
          ),
        reopen: (id) =>
          perform(() =>
            api.post<void>(path.inboxEntryReopen(workspaceId, id), {}, QUIET),
          ),
        snooze: (id, until) =>
          perform(() =>
            api.post<void>(
              path.inboxEntrySnooze(workspaceId, id),
              { until },
              QUIET,
            ),
          ),
        mute: (id, muted) =>
          perform(() =>
            api.post<void>(
              muted
                ? path.inboxEntryMute(workspaceId, id)
                : path.inboxEntryUnmute(workspaceId, id),
              {},
              QUIET,
            ),
          ),
        reply: (itemId, text) =>
          perform(async () => {
            await api.post<string>(
              path.itemComments(itemId),
              { contentMd: text, parentCommentId: null },
              { idempotencyKey: attemptKey(), ...QUIET },
            );
          }),
        replyOnPage: (pageId, text) =>
          perform(async () => {
            await api.post<string>(
              path.pageComments(pageId),
              { contentMd: text },
              { idempotencyKey: attemptKey(), ...QUIET },
            );
          }),
      },
    }),
    [api, perform, queryClient, userId, workspaceId],
  );
}
