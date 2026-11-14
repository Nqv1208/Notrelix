import { useMutation, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import type { createCardApi } from "../api/item.api";
import type { createLabelApi } from "../api/label.api";
import type { createChecklistApi } from "../api/checklist.api";
import type { createItemExtrasApi } from "../api/item-extras.api";
import { useWorkManagementServices } from "../services";

/** What the create dialog collects for a new item. */
export interface CreateItemWithDetailsInput {
  readonly listId: string;
  /**
   * Set for a sub-item: the new item is created under this parent, in its
   * group (`listId` is then not sent). Omitted for a top-level item.
   */
  readonly parentItemId?: string;
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
  /** Links to other items chosen while drafting (waits on, blocks, related). */
  readonly dependencies?: readonly {
    readonly itemId: string;
    readonly relation: "blocked-by" | "blocks" | "related";
  }[];
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
  | "dependencies"
  | "files";

/**
 * The parts of a create that are still to be written: a detail that failed,
 * with the list items already written left out. Retrying the missing part
 * writes exactly this, against the item that exists.
 */
export interface ItemDetailRemainder {
  statusOptionId?: string | null;
  laneValue?: CreateItemWithDetailsInput["laneValue"];
  priority?: CreateItemWithDetailsInput["priority"];
  assigneeIds?: readonly string[];
  labelIds?: readonly string[];
  fieldValues?: CreateItemWithDetailsInput["fieldValues"];
  checklists?: CreateItemWithDetailsInput["checklists"];
  links?: CreateItemWithDetailsInput["links"];
  dependencies?: CreateItemWithDetailsInput["dependencies"];
  files?: readonly File[];
}

export interface ItemDetailsOutcome {
  /** The details that could not be saved; the item exists without them. */
  readonly failed: readonly CreateItemDetail[];
  /** The item's version after the last write that answered. */
  readonly version: number;
  /** What is still missing (empty when `failed` is empty). */
  readonly remaining: ItemDetailRemainder;
}

export interface CreateItemWithDetailsResult extends ItemDetailsOutcome {
  /** The new item's id; undefined when the server sent none. */
  readonly itemId: string | undefined;
}

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
  /** `createChildItem` makes a sub-item; `addLink` makes item links. */
  readonly itemExtras?: Pick<
    ReturnType<typeof createItemExtrasApi>,
    "addLink" | "createChildItem"
  >;
}

/**
 * Writes the details of an item that exists, in order. A failed detail is
 * named and kept in `remaining` with the list items already written removed,
 * so a retry never repeats a write that succeeded.
 */
export async function writeItemDetails(
  { cards, labels, checklists, itemExtras }: ItemDetailWriters,
  itemId: string,
  startVersion: number,
  input: ItemDetailRemainder,
  options: { readonly resuming?: boolean } = {},
): Promise<ItemDetailsOutcome> {
  // Each write names the version the one before it returned.
  let version = startVersion;
  const advance = (written: { version?: number }) => {
    if (typeof written.version !== "number")
      throw new Error("The server sent no version after a write.");
    version = written.version;
  };
  const failed: CreateItemDetail[] = [];
  const remaining: ItemDetailRemainder = {};

  if (input.statusOptionId) {
    const optionId = input.statusOptionId;
    try {
      advance(await cards.updateStatus(itemId, optionId, version));
    } catch {
      failed.push("status");
      remaining.statusOptionId = optionId;
    }
  }
  if (input.laneValue) {
    const lane = input.laneValue;
    try {
      advance(
        await cards.updateFieldValue(
          {
            cardId: itemId,
            fieldDefinitionId: lane.fieldDefinitionId,
            value: lane.value,
          },
          { expectedVersion: version },
        ),
      );
    } catch {
      failed.push("lane");
      remaining.laneValue = lane;
    }
  }
  if (input.priority) {
    const priority = input.priority;
    try {
      advance(
        await cards.updateFieldValue(
          {
            cardId: itemId,
            fieldDefinitionId: priority.fieldDefinitionId,
            value: priority.value,
          },
          { expectedVersion: version },
        ),
      );
    } catch {
      failed.push("priority");
      remaining.priority = priority;
    }
  }
  if (input.assigneeIds?.length) {
    const assignees = input.assigneeIds;
    try {
      version = await cards.setAssignees(itemId, assignees, [], version);
    } catch {
      failed.push("assignees");
      remaining.assigneeIds = assignees;
    }
  }
  if (input.labelIds?.length) {
    const labelIds = input.labelIds;
    let done = 0;
    try {
      for (const labelId of labelIds) {
        await labels.addLabelToCard(itemId, labelId);
        done += 1;
      }
    } catch {
      failed.push("labels");
      remaining.labelIds = labelIds.slice(done);
    }
  }
  if (input.fieldValues?.length) {
    const values = input.fieldValues;
    let done = 0;
    try {
      for (const value of values) {
        advance(
          await cards.updateFieldValue(
            { cardId: itemId, ...value },
            { expectedVersion: version },
          ),
        );
        done += 1;
      }
    } catch {
      failed.push("fields");
      remaining.fieldValues = values.slice(done);
    }
  }
  if (input.checklists?.length && checklists) {
    const lists = input.checklists;
    let done = 0;
    try {
      for (const list of lists) {
        await writeChecklist(
          checklists,
          itemId,
          list,
          options.resuming ?? false,
        );
        done += 1;
      }
    } catch {
      failed.push("checklists");
      remaining.checklists = lists.slice(done);
    }
  }
  if (input.links?.length && cards.createCardLink) {
    const links = input.links;
    const addLink = cards.createCardLink;
    let done = 0;
    try {
      for (const link of links) {
        await addLink(itemId, link, { idempotencyKey: crypto.randomUUID() });
        done += 1;
      }
    } catch {
      failed.push("links");
      remaining.links = links.slice(done);
    }
  }
  if (input.dependencies?.length && itemExtras) {
    const dependencies = input.dependencies;
    let done = 0;
    try {
      for (const link of dependencies) {
        await itemExtras.addLink(itemId, link);
        done += 1;
      }
    } catch {
      failed.push("dependencies");
      remaining.dependencies = dependencies.slice(done);
    }
  }
  if (input.files?.length && cards.uploadCardFile) {
    const files = input.files;
    const upload = cards.uploadCardFile;
    let done = 0;
    try {
      for (const file of files) {
        await upload(itemId, file, { idempotencyKey: crypto.randomUUID() });
        done += 1;
      }
    } catch {
      failed.push("files");
      remaining.files = files.slice(done);
    }
  }
  return { failed, version, remaining };
}

/**
 * One checklist with its items. When resuming a create that failed part way,
 * a list already on the item with the same title is reused and its items
 * already present are skipped, so the retry never makes a second copy.
 */
async function writeChecklist(
  checklists: NonNullable<ItemDetailWriters["checklists"]>,
  itemId: string,
  list: NonNullable<CreateItemWithDetailsInput["checklists"]>[number],
  resuming: boolean,
) {
  const existing = resuming
    ? (await checklists.getChecklists(itemId)).find(
        (entry) => entry.title === list.title,
      )
    : undefined;
  let target = existing;
  if (!target) {
    await checklists.createChecklist({ cardId: itemId, title: list.title });
    // The create answers nothing: the list is the newest one of the item.
    target = (await checklists.getChecklists(itemId)).at(-1);
  }
  if (!target) return;
  const have = new Set(target.items.map((item) => item.title));
  for (const title of list.items) {
    if (have.has(title)) continue;
    const current = (await checklists.getChecklists(itemId)).find(
      (entry) => entry.id === target?.id,
    );
    await checklists.createChecklistItem({
      checklistId: target.id,
      title,
      expectedVersion: current?.version ?? target.version ?? 0,
    });
    have.add(title);
  }
}

/**
 * Creates an item with the facts the dialog collected. The item's own facts
 * (description, dates) travel with the create so it never exists half-filled;
 * status, priority, people and labels belong to other facts and use their own
 * writes afterwards. A sub-item is created under its parent with the same
 * facts. When one of those writes fails the item stays, the result names what
 * is missing, and the dialog can retry just that.
 */
export async function runCreateItemWithDetails(
  writers: ItemDetailWriters,
  boardId: string,
  input: CreateItemWithDetailsInput,
): Promise<CreateItemWithDetailsResult> {
  const { cards, itemExtras } = writers;
  const facts = {
    title: input.title,
    descriptionMd: input.descriptionMd,
    startDate: input.startDate,
    dueDate: input.dueDate,
  };
  const key = input.idempotencyKey ?? crypto.randomUUID();
  let created: { id: string; version: number } | undefined;
  if (input.parentItemId) {
    if (!itemExtras)
      throw new Error("Creating a sub-item needs the item extras service.");
    created = await itemExtras.createChildItem(input.parentItemId, facts, {
      idempotencyKey: key,
    });
  } else {
    created = await cards.createCard(
      boardId,
      { listId: input.listId, ...facts },
      { idempotencyKey: key },
    );
  }
  const itemId = created?.id;
  if (!created || !itemId)
    return { itemId: undefined, failed: [], version: 0, remaining: {} };
  const outcome = await writeItemDetails(
    writers,
    itemId,
    created.version,
    input,
  );
  return { itemId, ...outcome };
}

/** Retries the missing part of a create: writes only what `remaining` names, on the item that exists. */
export function useRetryItemDetails(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards, labels, checklists, itemExtras } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      readonly itemId: string;
      readonly version: number;
      readonly remaining: ItemDetailRemainder;
    }) =>
      writeItemDetails(
        { cards, labels, checklists, itemExtras },
        input.itemId,
        input.version,
        input.remaining,
        { resuming: true },
      ),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}

export function useCreateItemWithDetails(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards, labels, checklists, itemExtras } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: CreateItemWithDetailsInput) =>
      runCreateItemWithDetails(
        { cards, labels, checklists, itemExtras },
        boardId,
        input,
      ),
    onSettled: (_result, _error, input) => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
      // A sub-item shows under its parent at once.
      if (input.parentItemId)
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKey(
            workspaceId,
            "work-management",
            "item-children",
            input.parentItemId,
          ),
        });
    },
  });
}
