import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import type { createCardApi } from "../api/item.api";
import type { createLabelApi } from "../api/label.api";
import type { createChecklistApi } from "../api/checklist.api";
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
  /** Custom field values, written after the item exists. */
  readonly fieldValues?: readonly {
    readonly fieldDefinitionId: string;
    readonly value: unknown;
  }[];
  /** Checklists typed into the draft, in order. */
  readonly checklists?: readonly {
    readonly title: string;
    readonly items: readonly string[];
  }[];
  /** Web links attached while drafting. */
  readonly links?: readonly { readonly url: string; readonly name: string }[];
  /** Files chosen while drafting; uploaded once the item exists. */
  readonly files?: readonly File[];
  /**
   * One key per create attempt: a retry reuses it so the server never makes
   * a second item. Absent: a fresh key.
   */
  readonly idempotencyKey?: string;
}

/** A write that failed after the item itself was created. */
export type CreateItemDetail =
  | "status"
  | "lane"
  | "priority"
  | "assignees"
  | "labels"
  | "fields"
  | "checklists"
  | "links"
  | "files";

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
  > &
    Partial<
      Pick<
        ReturnType<typeof createCardApi>,
        "createCardLink" | "uploadCardFile"
      >
    >;
  readonly labels: Pick<ReturnType<typeof createLabelApi>, "addLabelToCard">;
  readonly checklists?: Pick<
    ReturnType<typeof createChecklistApi>,
    "createChecklist" | "getChecklists" | "createChecklistItem"
  >;
}

export async function runCreateItemWithDetails(
  { cards, labels, checklists }: ItemDetailWriters,
  boardId: string,
  input: CreateItemWithDetailsInput,
): Promise<CreateItemWithDetailsResult> {
  const created = await cards.createCard(
    boardId,
    {
      listId: input.listId,
      title: input.title,
      descriptionMd: input.descriptionMd,
      startDate: input.startDate,
      dueDate: input.dueDate,
    },
    { idempotencyKey: input.idempotencyKey ?? crypto.randomUUID() },
  );
  const itemId = created?.id;
  const failed: CreateItemDetail[] = [];
  if (itemId) {
    // Each write names the version the one before it returned (the create returns the first).
    let version = created.version;
    const advance = (written: { version?: number }) => {
      if (typeof written.version !== "number")
        throw new Error("The server sent no version after a write.");
      version = written.version;
    };
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
      await attempt("status", async () =>
        advance(await cards.updateStatus(itemId, optionId, version)),
      );
    }
    if (input.laneValue) {
      const lane = input.laneValue;
      await attempt("lane", async () =>
        advance(
          await cards.updateFieldValue(
            {
              cardId: itemId,
              fieldDefinitionId: lane.fieldDefinitionId,
              value: lane.value,
            },
            { expectedVersion: version },
          ),
        ),
      );
    }
    if (input.priority) {
      const priority = input.priority;
      await attempt("priority", async () =>
        advance(
          await cards.updateFieldValue(
            {
              cardId: itemId,
              fieldDefinitionId: priority.fieldDefinitionId,
              value: priority.value,
            },
            { expectedVersion: version },
          ),
        ),
      );
    }
    if (input.assigneeIds?.length) {
      const assignees = input.assigneeIds;
      await attempt("assignees", async () => {
        version = await cards.setAssignees(itemId, assignees, [], version);
      });
    }
    if (input.labelIds?.length) {
      const labelIds = input.labelIds;
      await attempt("labels", async () => {
        for (const labelId of labelIds) {
          await labels.addLabelToCard(itemId, labelId);
        }
      });
    }
    if (input.fieldValues?.length) {
      const values = input.fieldValues;
      await attempt("fields", async () => {
        for (const value of values) {
          advance(
            await cards.updateFieldValue(
              { cardId: itemId, ...value },
              { expectedVersion: version },
            ),
          );
        }
      });
    }
    if (input.checklists?.length && checklists) {
      const lists = input.checklists;
      await attempt("checklists", async () => {
        for (const list of lists) {
          await checklists.createChecklist({
            cardId: itemId,
            title: list.title,
          });
          // The create answers nothing: the list is the newest one of the item.
          const created = (await checklists.getChecklists(itemId)).at(-1);
          if (!created) continue;
          for (const title of list.items) {
            const version =
              (await checklists.getChecklists(itemId)).find(
                (entry) => entry.id === created.id,
              )?.version ??
              created.version ??
              0;
            await checklists.createChecklistItem({
              checklistId: created.id,
              title,
              expectedVersion: version,
            });
          }
        }
      });
    }
    if (input.links?.length && cards.createCardLink) {
      const links = input.links;
      const addLink = cards.createCardLink;
      await attempt("links", async () => {
        for (const link of links)
          await addLink(itemId, link, { idempotencyKey: crypto.randomUUID() });
      });
    }
    if (input.files?.length && cards.uploadCardFile) {
      const files = input.files;
      const upload = cards.uploadCardFile;
      await attempt("files", async () => {
        for (const file of files)
          await upload(itemId, file, { idempotencyKey: crypto.randomUUID() });
      });
    }
  }
  return { itemId, failed };
}

export function useCreateItemWithDetails(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards, labels, checklists } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: CreateItemWithDetailsInput) =>
      runCreateItemWithDetails({ cards, labels, checklists }, boardId, input),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}
