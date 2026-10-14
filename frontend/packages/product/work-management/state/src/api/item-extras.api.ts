import type { NotrelixClient } from "@notrelix/contracts";

/**
 * Item sheet reads and writes beyond the generated contract (gap
 * `CTR-GAP-WS-ITEM-SHEET`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Board/Kanban"): the child items of an item and
 * whether the member follows it. Follow and reopen share the My work gap
 * (`CTR-GAP-WS-MYWORK-EXT`).
 */
export interface ItemChildItemDto {
  readonly id: string;
  readonly key: string | null;
  readonly title: string;
  readonly done: boolean;
  readonly dueAt: string | null;
  readonly assignee: {
    readonly id: string;
    readonly userId: string;
    readonly name: string;
    readonly initials: string;
  } | null;
}

/** A phase or a milestone of a board (gap `CTR-GAP-WM-PLAN-ITEMS`). */
export interface BoardPlanItemDto {
  readonly id: string;
  readonly kind: "phase" | "milestone";
  readonly title: string;
  /** First and last Hanoi day "yyyy-MM-dd"; equal for a milestone. */
  readonly startDay: string;
  readonly endDay: string;
  readonly category: "todo" | "doing" | "review" | "done";
}

export function createItemExtrasApi(client: NotrelixClient) {
  const { api } = client;
  const quiet = { skipGlobalErrorToast: true } as const;
  const key = () => crypto.randomUUID();
  return {
    async getPlanItems(boardId: string): Promise<BoardPlanItemDto[]> {
      const dto = await api.get<{ items?: BoardPlanItemDto[] } | null>(
        `/boards/${boardId}/plan-items`,
      );
      return dto?.items ?? [];
    },
    async getChildItems(itemId: string): Promise<ItemChildItemDto[]> {
      const dto = await api.get<{ items?: ItemChildItemDto[] } | null>(
        `/board-items/${itemId}/child-items`,
      );
      return dto?.items ?? [];
    },
    async getFollowing(itemId: string): Promise<boolean> {
      const dto = await api.get<{ following?: boolean } | null>(
        `/board-items/${itemId}/following`,
      );
      return dto?.following === true;
    },
    async setFollowing(itemId: string, following: boolean): Promise<void> {
      if (following)
        await api.put<void>(
          `/board-items/${itemId}/following`,
          undefined,
          quiet,
        );
      else await api.delete<void>(`/board-items/${itemId}/following`, quiet);
    },
    /**
     * Moves an item to another board (gap `CTR-GAP-WM-TABLE-BULK`, see
     * "BE requirement from FE: Table"): it lands in the target group of the
     * same name, else the first, with a new item key.
     */
    async moveToBoard(itemId: string, boardId: string): Promise<void> {
      await api.post<void>(
        `/board-items/${itemId}/move-to-board`,
        { boardId },
        { idempotencyKey: key(), ...quiet },
      );
    },
    /** Ticks (`done`) or unticks a child item. */
    async setChildDone(childId: string, done: boolean): Promise<void> {
      if (done)
        await api.post<void>(
          `/board-items/${childId}/complete`,
          {},
          { idempotencyKey: key(), ...quiet },
        );
      else
        await api.post<void>(
          `/board-items/${childId}/reopen`,
          {},
          { idempotencyKey: key(), ...quiet },
        );
    },
  };
}
