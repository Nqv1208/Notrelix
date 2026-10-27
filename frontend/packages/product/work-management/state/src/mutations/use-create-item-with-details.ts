import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import type { createCardApi } from "../api/item.api";
import type { createLabelApi } from "../api/label.api";
import { useWorkManagementServices } from "../services";

/** What the create dialog collects for a new item. */
export interface CreateItemWithDetailsInput {
  readonly listId: string;
  readonly title: string;
  readonly descriptionMd?: string;
  readonly startDate?: string;
  readonly dueDate?: string;
  readonly statusOptionId?: string | null;
  readonly priority?: {
    readonly fieldDefinitionId: string;
    readonly value: string;
  } | null;
  readonly assigneeIds?: readonly string[];
  readonly labelIds?: readonly string[];
  /**
   * The value of the field the board is grouped by (not Status) that puts the
   * item in its lane; written after the item exists, like status and priority.
   */
  readonly laneValue?: {
    readonly fieldDefinitionId: string;
    readonly value: string;
  } | null;
}

/** A write that failed after the item itself was created. */
export type CreateItemDetail =
  "status" | "lane" | "priority" | "assignees" | "labels";

export interface CreateItemWithDetailsResult {
  /** The new item's id; undefined when the server sent none. */
  readonly itemId: string | undefined;
  /** The details that could not be saved; the item exists without them. */
  readonly failed: readonly CreateItemDetail[];
}

/**
 * Creates an item with the facts the dialog collected. The item's own facts
 * (description, dates) travel with the create so it never exists half-filled;
 * status, priority, people and labels belong to other facts and use their own
 * writes afterwards. When one of those fails the item stays, the result names
 * what is missing, and the board is read again either way.
 */
/** The writes the create dialog makes; the services they use are passed in. */
export interface ItemDetailWriters {
  readonly cards: Pick<
    ReturnType<typeof createCardApi>,
    "createCard" | "updateStatus" | "updateFieldValue" | "setAssignees"
  >;
  readonly labels: Pick<ReturnType<typeof createLabelApi>, "addLabelToCard">;
}

export async function runCreateItemWithDetails(
  { cards, labels }: ItemDetailWriters,
  boardId: string,
  input: CreateItemWithDetailsInput,
): Promise<CreateItemWithDetailsResult> {
  const itemId = await cards.createCard(
    boardId,
    {
      listId: input.listId,
      title: input.title,
      descriptionMd: input.descriptionMd,
      startDate: input.startDate,
      dueDate: input.dueDate,
    },
    { idempotencyKey: crypto.randomUUID() },
  );
  const failed: CreateItemDetail[] = [];
  if (itemId) {
    const attempt = async (
      detail: CreateItemDetail,
      write: () => Promise<unknown>,
    ) => {
      try {
        await write();
      } catch {
        failed.push(detail);
      }
    };
    if (input.statusOptionId) {
      const optionId = input.statusOptionId;
      await attempt("status", () => cards.updateStatus(itemId, optionId));
    }
    if (input.laneValue) {
      const lane = input.laneValue;
      await attempt("lane", () =>
        cards.updateFieldValue({
          cardId: itemId,
          fieldDefinitionId: lane.fieldDefinitionId,
          value: lane.value,
        }),
      );
    }
    if (input.priority) {
      const priority = input.priority;
      await attempt("priority", () =>
        cards.updateFieldValue({
          cardId: itemId,
          fieldDefinitionId: priority.fieldDefinitionId,
          value: priority.value,
        }),
      );
    }
    if (input.assigneeIds?.length) {
      const assignees = input.assigneeIds;
      await attempt("assignees", () =>
        cards.setAssignees(itemId, assignees, []),
      );
    }
    if (input.labelIds?.length) {
      const labelIds = input.labelIds;
      await attempt("labels", async () => {
        for (const labelId of labelIds) {
          await labels.addLabelToCard(itemId, labelId);
        }
      });
    }
  }
  return { itemId, failed };
}

export function useCreateItemWithDetails(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards, labels } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: CreateItemWithDetailsInput) =>
      runCreateItemWithDetails({ cards, labels }, boardId, input),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}
