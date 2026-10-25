/**
 * The member's work feed (Home, My work, Inbox).
 *
 * The My work read (`WorkManagement.BoardItems.GetMyWorkExtended`), the Home
 * feed (`WorkManagement.BoardItems.GetHomeFeed`) and the inbox summary
 * (`Collaboration.Notifications.GetInboxSummary`) are bound to the generated
 * contract and mapped below into the DTO types the pages draw. Every other
 * read and write here is still a registered gap (`CTR-GAP-WS-MYWORK-EXT`,
 * `CTR-GAP-WS-HOME-FEED`, `CTR-GAP-WS-INBOX` in
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
      requestedBy && raw.approval?.requestedAt
        ? { requestedBy, requestedAt: raw.approval.requestedAt }
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
              note: raw.note ?? "",
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
