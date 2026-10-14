/**
 * Work Management operations the workspace pages compose (WX-HOME-001,
 * WX-HOME-002, WX-DIR-001): the My Work read, completing an Item and
 * creating a Board.
 *
 * TRANSITION: these adapters belong to `@notrelix/work-management-state`,
 * which this phase may not edit (its table files carry another session's
 * uncommitted work). They stay thin — producer calls, cache effects, no
 * business rules — and move into that package with WX-BOARD-003. New code
 * must not add Work Management behavior here.
 */
import { useCallback, useMemo } from "react";
import {
  useInfiniteQuery,
  useMutation,
  useQueryClient,
  type InfiniteData,
} from "@tanstack/react-query";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { MyWorkItem } from "@notrelix/features-workspace/core";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import { wmQueryKeys } from "@notrelix/work-management-state";
import { boardPlacementsQueryKey } from "./board-placement-bridge";

type MyWorkPageDto = OperationResponse<
  "WorkManagement.BoardItems.ListMyWork",
  200
>;
type CompleteBody = OperationRequestBody<"WorkManagement.BoardItems.Complete">;
type CreateBoardBody = OperationRequestBody<"WorkManagement.Boards.Create">;

export interface MyWorkPage {
  readonly items: readonly MyWorkItem[];
  readonly nextCursor: string | null;
}

/** Page size of the My Work read (producer default 50, maximum 100). */
export const MY_WORK_PAGE_SIZE = 50;

export const myWorkQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "my-work");

function toMyWorkPage(dto: MyWorkPageDto | null): MyWorkPage {
  const items: MyWorkItem[] = [];
  for (const raw of dto?.items ?? []) {
    if (!raw.id || !raw.boardId) continue;
    items.push({
      id: raw.id,
      boardId: raw.boardId,
      groupId: raw.groupId ?? "",
      name: raw.name ?? "",
      dueAt: raw.dueAt ?? null,
      startedAt: raw.startedAt ?? null,
      updatedAt: raw.updatedAt ?? null,
      version: raw.version ?? 0,
    });
  }
  return {
    items,
    nextCursor: dto?.hasNextPage && dto.nextCursor ? dto.nextCursor : null,
  };
}

/** `WorkManagement.BoardItems.ListMyWork`, cursor-paged. */
export function useMyWorkItems(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useInfiniteQuery({
    queryKey: myWorkQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    initialPageParam: null as string | null,
    queryFn: async ({ pageParam }) => {
      const query = new URLSearchParams({ limit: String(MY_WORK_PAGE_SIZE) });
      if (pageParam) query.set("cursor", pageParam);
      const dto = await client.api.get<MyWorkPageDto | null>(
        `${client.endpoints.workspaces.myWorkItems(workspaceId)}?${query}`,
      );
      return toMyWorkPage(dto);
    },
    getNextPageParam: (last) => last.nextCursor ?? undefined,
  });
}

/** Flattens loaded pages, keeping the producer order. */
export function flattenMyWork(
  data: InfiniteData<MyWorkPage> | undefined,
): MyWorkItem[] {
  return data?.pages.flatMap((page) => [...page.items]) ?? [];
}

/**
 * `WorkManagement.BoardItems.Complete`. One idempotency key per completion
 * attempt (a replay returns the stored result). Not optimistic: the row shows
 * progress and leaves the list only after the producer confirms.
 */
export function useCompleteItem(workspaceId: string) {
  const { api: client } = useAppRuntime();
  const queryClient = useQueryClient();
  const key = myWorkQueryKey(workspaceId);

  return useMutation({
    mutationFn: async ({
      item,
      attemptKey,
    }: {
      item: MyWorkItem;
      attemptKey: string;
    }) => {
      const body: CompleteBody = {};
      await client.api.post<void>(
        client.endpoints.boardItems.complete(item.id),
        body,
        { idempotencyKey: attemptKey, skipGlobalErrorToast: true },
      );
    },
    onSuccess: (_result, { item }) => {
      queryClient.setQueryData<InfiniteData<MyWorkPage>>(key, (data) =>
        data
          ? {
              ...data,
              pages: data.pages.map((page) => ({
                ...page,
                items: page.items.filter((entry) => entry.id !== item.id),
              })),
            }
          : data,
      );
    },
    onSettled: (_result, _error, { item }) => {
      void queryClient.invalidateQueries({ queryKey: key });
      // The work feed's reads (CTR-GAP-WS-*) show the same item.
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(
          workspaceId,
          "work-management",
          "my-work-extended",
        ),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "workspace-feed"),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, item.boardId),
      });
    },
  });
}

/**
 * `WorkManagement.Boards.Create` (201 with the new Board id). One idempotency
 * key per submit, so a network retry of the same submit cannot create twice.
 */
export function useCreateBoard(workspaceId: string) {
  const { api: client } = useAppRuntime();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      title,
      attemptKey,
      spaceId = null,
    }: {
      title: string;
      attemptKey: string;
      /** Create the Board in this Space (OT-5); `null` = workspace level. */
      spaceId?: string | null;
    }): Promise<string | null> => {
      const body: CreateBoardBody = spaceId ? { title, spaceId } : { title };
      const id = await client.api.post<unknown>(
        client.endpoints.boards.listByWorkspaceId(workspaceId),
        body,
        { idempotencyKey: attemptKey, skipGlobalErrorToast: true },
      );
      return typeof id === "string" ? id : null;
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: wmQueryKeys.workspaceList(workspaceId),
      });
      await queryClient.invalidateQueries({
        queryKey: boardPlacementsQueryKey(workspaceId),
      });
    },
  });
}

/** A fresh idempotency key per user-initiated attempt. */
export function useAttemptKey(): () => string {
  return useCallback(() => crypto.randomUUID(), []);
}

/** Board names by id from the authorized Board list (for My work rows). */
export function useBoardNames(
  boards: readonly { id: string; title: string }[] | undefined,
): ReadonlyMap<string, string> {
  return useMemo(
    () => new Map((boards ?? []).map((board) => [board.id, board.title])),
    [boards],
  );
}
