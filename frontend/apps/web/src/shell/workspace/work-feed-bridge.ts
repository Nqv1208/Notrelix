/**
 * The member's work feed beyond the generated contract (Home, My work, Inbox).
 *
 * Every read and write here is a registered gap (`CTR-GAP-WS-MYWORK-EXT`,
 * `CTR-GAP-WS-HOME-FEED`, `CTR-GAP-WS-INBOX` in
 * `tooling/contracts/enabled-consumer-surface.ts`): the shape the pages need,
 * served by the mock backend until the producer builds it from the
 * requirement in `docs/superpowers/be-blockers-2026-10.md` ("BE requirement
 * from FE"). The DTO types below are the frontend's own and the single place
 * the shapes live; nothing under `packages/foundation/contracts/src/generated`
 * is touched.
 *
 * TRANSITION: like `work-management-bridge.ts`, these adapters stay thin
 * (producer calls and cache effects, no business rules) and move to the owning
 * product packages once the producer contract exists.
 */
import { useCallback, useMemo } from "react";
import {
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
import { useAppRuntime } from "@notrelix/runtime-web";
import { wmQueryKeys } from "@notrelix/work-management-state";
import { myWorkQueryKey } from "./work-management-bridge";

// ─── DTOs (frontend-owned; see the BE requirement log for the contract) ──────

export type WorkBucketDto = "todo" | "doing" | "review" | "done";
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
  | "mention"
  | "overdue"
  | "assigned"
  | "changes_bundle"
  | "board_invited";

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
  readonly following: boolean;
  readonly muted: boolean;
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

export type BulkActionDto =
  | { readonly type: "set-due"; readonly dueAt: string | null }
  | { readonly type: "set-status"; readonly bucket: WorkBucketDto }
  | { readonly type: "unfollow" }
  | { readonly type: "assign"; readonly assigneeId: string };

export interface BulkResultDto {
  readonly results: readonly {
    readonly itemId: string;
    readonly ok: boolean;
    readonly error: "not-found" | "invalid" | null;
  }[];
}

// ─── Paths ───────────────────────────────────────────────────────────────────

const path = {
  myWorkExtended: (workspaceId: string) =>
    `/workspaces/${workspaceId}/my-work/extended`,
  myWorkBulk: (workspaceId: string) =>
    `/workspaces/${workspaceId}/my-work/bulk`,
  myWorkItems: (workspaceId: string) =>
    `/workspaces/${workspaceId}/my-work/items`,
  approval: (itemId: string) => `/board-items/${itemId}/approval`,
  reopen: (itemId: string) => `/board-items/${itemId}/reopen`,
  statusBucket: (itemId: string) => `/board-items/${itemId}/status-bucket`,
  following: (itemId: string) => `/board-items/${itemId}/following`,
  remind: (itemId: string) => `/board-items/${itemId}/remind`,
  homeFeed: (workspaceId: string) => `/workspaces/${workspaceId}/home/feed`,
  mentionReply: (mentionId: string) => `/mentions/${mentionId}/reply`,
  inbox: (workspaceId: string) => `/workspaces/${workspaceId}/inbox`,
  inboxReadAll: (workspaceId: string) =>
    `/workspaces/${workspaceId}/inbox/read-all`,
  inboxEntry: (id: string, action: string) => `/inbox/${id}/${action}`,
};

export const myWorkExtendedQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "my-work-extended");
export const homeFeedQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "workspace-feed", "home");
export const inboxQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "workspace-feed", "inbox");

const QUIET = { skipGlobalErrorToast: true } as const;

// ─── Reads ───────────────────────────────────────────────────────────────────

export function useMyWorkExtended(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: myWorkExtendedQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    queryFn: async () => {
      const dto = await client.api.get<MyWorkExtendedDto | null>(
        path.myWorkExtended(workspaceId),
      );
      return dto ?? { items: [], done: [], waiting: [], departments: {} };
    },
  });
}

export function useHomeFeed(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: homeFeedQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    queryFn: async () => {
      const dto = await client.api.get<HomeFeedDto | null>(
        path.homeFeed(workspaceId),
      );
      return (
        dto ?? {
          replies: [],
          events: [],
          summary: { approvalsPending: 0, mentionsUnread: 0 },
        }
      );
    },
  });
}

/** Unread inbox entries per workspace and in total (rail and sidebar badges). */
export function useInboxSummary() {
  const { api: client } = useAppRuntime();
  const accountId = useAccountId();
  return useQuery({
    queryKey: accountQueryKey(accountId, "workspace-feed", "inbox-summary"),
    staleTime: 15_000,
    queryFn: async () =>
      (await client.api.get<InboxSummaryDto | null>("/inbox/summary")) ?? {
        perWorkspace: [],
        total: 0,
      },
  });
}

export function useWorkspaceInbox(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: inboxQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 15_000,
    queryFn: async () => {
      const dto = await client.api.get<InboxDto | null>(
        path.inbox(workspaceId),
      );
      return (
        dto ?? {
          workspace: { id: workspaceId, name: "" },
          items: [],
          counts: {
            need: 0,
            mention: 0,
            assigned: 0,
            follow: 0,
            all: 0,
            unread: 0,
          },
        }
      );
    },
  });
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

/** One idempotency key per user-initiated attempt. */
function attemptKey(): string {
  return crypto.randomUUID();
}

export interface WorkFeedWrites {
  readonly decideApproval: (input: {
    itemId: string;
    boardId: string;
    decision: "approve" | "request-changes";
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
  }) => Promise<{ id: string; key: string | null }>;
  readonly replyToMention: (input: {
    mentionId: string;
    text: string;
  }) => Promise<void>;
  readonly inbox: {
    readonly handled: (id: string) => Promise<void>;
    readonly snooze: (id: string, until: string) => Promise<void>;
    readonly mute: (id: string) => Promise<void>;
    readonly reply: (id: string, text: string) => Promise<void>;
    readonly readAll: () => Promise<void>;
  };
}

/**
 * Every write of the work feed. Not optimistic: the producer answers, then the
 * reads of the three pages and the Board cache refetch (one source of truth).
 */
export function useWorkFeedWrites(workspaceId: string): WorkFeedWrites {
  const { api: client } = useAppRuntime();
  const queryClient = useQueryClient();
  const accountId = useAccountId();
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
      decideApproval: ({ itemId, boardId, decision, comment }) =>
        perform(
          () =>
            api.post<void>(
              path.approval(itemId),
              { decision, comment: comment ?? null },
              { idempotencyKey: attemptKey(), ...QUIET },
            ),
          [boardId],
        ),
      reopen: ({ itemId, boardId }) =>
        perform(
          () =>
            api.post<void>(
              path.reopen(itemId),
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
          () => api.put<void>(path.statusBucket(itemId), { bucket }, QUIET),
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
        perform(
          () =>
            api.post<BulkResultDto>(
              path.myWorkBulk(workspaceId),
              { itemIds, action },
              { idempotencyKey: attemptKey(), ...QUIET },
            ),
          boardIds,
        ),
      createItem: ({ boardId, name }) =>
        perform(
          () =>
            api.post<{ id: string; key: string | null }>(
              path.myWorkItems(workspaceId),
              { boardId, name },
              { idempotencyKey: attemptKey(), ...QUIET },
            ),
          [boardId],
        ),
      replyToMention: ({ mentionId, text }) =>
        perform(() =>
          api.post<void>(
            path.mentionReply(mentionId),
            { text },
            {
              idempotencyKey: attemptKey(),
              ...QUIET,
            },
          ),
        ),
      inbox: {
        handled: (id) =>
          perform(() =>
            api.post<void>(path.inboxEntry(id, "handled"), {}, QUIET),
          ),
        snooze: (id, until) =>
          perform(() =>
            api.post<void>(path.inboxEntry(id, "snooze"), { until }, QUIET),
          ),
        mute: (id) =>
          perform(() => api.post<void>(path.inboxEntry(id, "mute"), {}, QUIET)),
        reply: (id, text) =>
          perform(() =>
            api.post<void>(
              path.inboxEntry(id, "reply"),
              { text },
              { idempotencyKey: attemptKey(), ...QUIET },
            ),
          ),
        readAll: () =>
          perform(() =>
            api.post<void>(path.inboxReadAll(workspaceId), {}, QUIET),
          ),
      },
    }),
    [api, perform, workspaceId],
  );
}
