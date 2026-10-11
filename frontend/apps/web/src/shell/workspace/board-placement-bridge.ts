/**
 * Board placement in Spaces (OT-5, GOV-028): which Space each Board is in,
 * and moving a Board into or out of a Space.
 *
 * TRANSITION (same owner and exit as `work-management-bridge.ts`): these
 * adapters belong to `@notrelix/work-management-state`, whose Board model
 * drops `spaceId` and `version` and which this phase may not edit (its files
 * carry another session's uncommitted work). They stay thin — producer calls
 * and cache effects, no business rules — and move into that package with
 * WX-BOARD-003. The placement read uses the same producer list as
 * `useWorkspaceBoards` under its own key until the Board model carries both
 * fields.
 */
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import { wmQueryKeys } from "@notrelix/work-management-state";

type BoardListDto = OperationResponse<
  "WorkManagement.Boards.ListWorkspaceBoards",
  200
>;
type MoveBoardBody = OperationRequestBody<"WorkManagement.Boards.MoveToSpace">;

export interface BoardPlacement {
  readonly id: string;
  /** `null` at the workspace level, or when the Space is not visible. */
  readonly spaceId: string | null;
  /** The Board aggregate version (`expectedVersion` of a move); 0 = unknown. */
  readonly version: number;
}

export const boardPlacementsQueryKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "board-placements");

function toPlacements(dto: BoardListDto | null): BoardPlacement[] {
  const out: BoardPlacement[] = [];
  for (const board of dto ?? []) {
    if (!board.id) continue;
    out.push({
      id: board.id,
      spaceId: board.spaceId ?? null,
      version: typeof board.version === "number" ? board.version : 0,
    });
  }
  return out;
}

/** `WorkManagement.Boards.ListWorkspaceBoards`, keeping placement fields. */
export function useBoardPlacements(workspaceId: string) {
  const { api: client } = useAppRuntime();
  return useQuery({
    queryKey: boardPlacementsQueryKey(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
    queryFn: async () =>
      toPlacements(
        await client.api.get<BoardListDto | null>(
          client.endpoints.boards.listByWorkspaceId(workspaceId),
        ),
      ),
  });
}

export interface MoveBoardInput {
  readonly boardId: string;
  /** `null` moves the Board back to the workspace level. */
  readonly spaceId: string | null;
  readonly expectedVersion: number;
  /** One idempotency key per user-initiated move attempt. */
  readonly attemptKey: string;
}

/**
 * `WorkManagement.Boards.MoveToSpace` (expected version, idempotency key).
 * Optimistic: the placement is patched at once and rolled back if the
 * producer rejects the move (stale version, no right on the target). Every
 * outcome refreshes the placements and the Board list.
 */
export function useMoveBoardToSpace(workspaceId: string) {
  const { api: client } = useAppRuntime();
  const queryClient = useQueryClient();
  const key = boardPlacementsQueryKey(workspaceId);

  return useMutation({
    retry: false,
    mutationFn: async (input: MoveBoardInput) => {
      const body: MoveBoardBody = {
        spaceId: input.spaceId,
        expectedVersion: input.expectedVersion,
      };
      await client.api.post<void>(
        client.endpoints.boards.moveToSpace(input.boardId),
        body,
        { idempotencyKey: input.attemptKey, skipGlobalErrorToast: true },
      );
    },
    onMutate: async (input: MoveBoardInput) => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<BoardPlacement[]>(key);
      if (previous) {
        queryClient.setQueryData<BoardPlacement[]>(
          key,
          previous.map((placement) =>
            placement.id === input.boardId
              ? { ...placement, spaceId: input.spaceId }
              : placement,
          ),
        );
      }
      return { previous };
    },
    onError: (_error, _input, context) => {
      if (context?.previous) queryClient.setQueryData(key, context.previous);
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: key });
      await queryClient.invalidateQueries({
        queryKey: wmQueryKeys.workspaceList(workspaceId),
      });
    },
  });
}
