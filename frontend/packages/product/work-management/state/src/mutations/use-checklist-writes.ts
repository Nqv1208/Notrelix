import { useCallback, useEffect, useMemo, useSyncExternalStore } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import {
  PENDING_CHECKLIST_ID_PREFIX,
  moveById,
  placementOf,
  type Checklist,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import {
  CHECKLIST_UNDO_MS,
  checklistQueueFor,
  type ChecklistOp,
} from "./checklist-queue";

const pendingId = () => `${PENDING_CHECKLIST_ID_PREFIX}${crypto.randomUUID()}`;
const opId = (kind: string) => `${kind}:${crypto.randomUUID()}`;

const mapList = (
  lists: Checklist[],
  id: string,
  change: (list: Checklist) => Checklist,
): Checklist[] => lists.map((list) => (list.id === id ? change(list) : list));

/**
 * Every write of an item's checklists, optimistic and one after another per
 * item (see `ChecklistQueue`). The checklists themselves are read from the item
 * (`useCard`); this hook is the write side and its failure, conflict and undo
 * state.
 */
export function useChecklistWrites(
  cardId: string,
  boardId: string,
  workspaceId: string,
  /** The checklists as the screen shows them now. */
  checklists: readonly Checklist[],
) {
  const queryClient = useQueryClient();
  const { checklists: api, itemExtras } = useWorkManagementServices();
  const queue = useMemo(
    () => checklistQueueFor(queryClient, workspaceId, boardId, cardId),
    [queryClient, workspaceId, boardId, cardId],
  );
  const state = useSyncExternalStore(
    queue.subscribe,
    queue.getSnapshot,
    queue.getSnapshot,
  );

  // Leaving the screen sends a delete that was waiting for its undo.
  useEffect(() => () => queue.releaseDelayed(), [queue]);

  const addList = useCallback(
    (title: string) => {
      const text = title.trim();
      if (!text) return;
      const id = pendingId();
      queue.schedule({
        id: opId("add-list"),
        kind: "add-list",
        text,
        apply: (lists) => [
          ...lists,
          {
            id,
            title: text,
            position: (lists.at(-1)?.position ?? 0) + 1,
            items: [],
          },
        ],
        send: () => api.createChecklist({ cardId, title: text }),
      });
    },
    [api, cardId, queue],
  );

  const renameList = useCallback(
    (checklistId: string, title: string) => {
      const text = title.trim();
      const current = checklists.find((list) => list.id === checklistId);
      if (!text || !current || current.title === text) return;
      queue.schedule({
        id: opId("rename-list"),
        kind: "rename-list",
        checklistId,
        text,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({ ...list, title: text })),
        send: (version) =>
          api.updateChecklist({
            checklistId,
            title: text,
            expectedVersion: version ?? 0,
          }),
      });
    },
    [api, checklists, queue],
  );

  const deleteList = useCallback(
    (checklistId: string) => {
      const index = checklists.findIndex((list) => list.id === checklistId);
      const removed = checklists[index];
      if (!removed) return;
      queue.schedule(
        {
          id: opId("delete-list"),
          kind: "delete-list",
          checklistId,
          text: removed.title,
          apply: (lists) => lists.filter((list) => list.id !== checklistId),
          send: () => api.deleteChecklist(checklistId),
        },
        {
          delayMs: CHECKLIST_UNDO_MS,
          restore: (lists) => {
            const at = Math.min(index, lists.length);
            return [...lists.slice(0, at), removed, ...lists.slice(at)];
          },
        },
      );
    },
    [api, checklists, queue],
  );

  const moveList = useCallback(
    (checklistId: string, toIndex: number) => {
      const moved = moveById(checklists, checklistId, toIndex);
      if (moved.every((list, i) => list.id === checklists[i]?.id)) return;
      queue.schedule({
        id: opId("move-list"),
        kind: "move-list",
        checklistId,
        apply: (lists) => moveById(lists, checklistId, toIndex),
        send: (version) =>
          api.moveChecklist({
            checklistId,
            expectedVersion: version ?? 0,
            ...placementOf(moved, checklistId),
          }),
      });
    },
    [api, checklists, queue],
  );

  const addItem = useCallback(
    (checklistId: string, title: string) => {
      const text = title.trim();
      if (!text) return;
      const id = pendingId();
      queue.schedule({
        id: opId("add-item"),
        kind: "add-item",
        checklistId,
        itemId: id,
        text,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({
            ...list,
            items: [
              ...list.items,
              {
                id,
                title: text,
                isDone: false,
                position: (list.items.at(-1)?.position ?? 0) + 1,
              },
            ],
          })),
        send: (version) =>
          api.createChecklistItem({
            checklistId,
            title: text,
            expectedVersion: version ?? 0,
          }),
      });
    },
    [api, queue],
  );

  const renameItem = useCallback(
    (checklistId: string, itemId: string, title: string) => {
      const text = title.trim();
      const current = checklists
        .find((list) => list.id === checklistId)
        ?.items.find((item) => item.id === itemId);
      if (!text || !current || current.title === text) return;
      queue.schedule({
        id: opId("rename-item"),
        kind: "rename-item",
        checklistId,
        itemId,
        text,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({
            ...list,
            items: list.items.map((item) =>
              item.id === itemId ? { ...item, title: text } : item,
            ),
          })),
        send: (version) =>
          api.renameChecklistItem({
            itemId,
            title: text,
            expectedVersion: version ?? 0,
          }),
      });
    },
    [api, checklists, queue],
  );

  const toggleItem = useCallback(
    (checklistId: string, itemId: string) => {
      const current = checklists
        .find((list) => list.id === checklistId)
        ?.items.find((item) => item.id === itemId);
      queue.schedule({
        id: opId("toggle-item"),
        kind: "toggle-item",
        checklistId,
        itemId,
        text: current?.title,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({
            ...list,
            items: list.items.map((item) =>
              item.id === itemId ? { ...item, isDone: !item.isDone } : item,
            ),
          })),
        send: (version) =>
          api.toggleChecklistItem({ itemId, expectedVersion: version ?? 0 }),
      });
    },
    [api, checklists, queue],
  );

  const setItemAssignee = useCallback(
    (checklistId: string, itemId: string, assigneeId: string | null) => {
      const current = checklists
        .find((list) => list.id === checklistId)
        ?.items.find((item) => item.id === itemId);
      if (!current || (current.assigneeId ?? null) === assigneeId) return;
      queue.schedule({
        id: opId("assign-item"),
        kind: "assign-item",
        checklistId,
        itemId,
        text: current.title,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({
            ...list,
            items: list.items.map((item) =>
              item.id === itemId ? { ...item, assigneeId } : item,
            ),
          })),
        send: (version) =>
          api.setChecklistItemAssignee({
            itemId,
            assigneeUserId: assigneeId,
            expectedVersion: version ?? 0,
          }),
      });
    },
    [api, checklists, queue],
  );

  const setItemDue = useCallback(
    (checklistId: string, itemId: string, dueAt: string | null) => {
      const current = checklists
        .find((list) => list.id === checklistId)
        ?.items.find((item) => item.id === itemId);
      if (!current || (current.dueAt ?? null) === dueAt) return;
      queue.schedule({
        id: opId("due-item"),
        kind: "due-item",
        checklistId,
        itemId,
        text: current.title,
        apply: (lists) =>
          mapList(lists, checklistId, (list) => ({
            ...list,
            items: list.items.map((item) =>
              item.id === itemId ? { ...item, dueAt } : item,
            ),
          })),
        send: (version) =>
          api.setChecklistItemDue({
            itemId,
            dueAt,
            expectedVersion: version ?? 0,
          }),
      });
    },
    [api, checklists, queue],
  );

  /** Removes an item at once; the request waits for "Hoàn tác". */
  const deleteItem = useCallback(
    (
      checklistId: string,
      itemId: string,
      kind: ChecklistOp["kind"] = "delete-item",
    ) => {
      const list = checklists.find((entry) => entry.id === checklistId);
      const index = list?.items.findIndex((item) => item.id === itemId) ?? -1;
      const removed = list?.items[index];
      if (!list || !removed) return;
      const undoable = kind === "delete-item";
      queue.schedule(
        {
          id: opId(kind),
          kind,
          checklistId,
          itemId,
          text: removed.title,
          apply: (lists) =>
            mapList(lists, checklistId, (entry) => ({
              ...entry,
              items: entry.items.filter((item) => item.id !== itemId),
            })),
          send: (version) =>
            api.deleteChecklistItem({ itemId, expectedVersion: version ?? 0 }),
        },
        undoable
          ? {
              delayMs: CHECKLIST_UNDO_MS,
              restore: (lists) =>
                mapList(lists, checklistId, (entry) => {
                  const at = Math.min(index, entry.items.length);
                  return {
                    ...entry,
                    items: [
                      ...entry.items.slice(0, at),
                      removed,
                      ...entry.items.slice(at),
                    ],
                  };
                }),
            }
          : {},
      );
    },
    [api, checklists, queue],
  );

  const moveItem = useCallback(
    (checklistId: string, itemId: string, toIndex: number) => {
      const list = checklists.find((entry) => entry.id === checklistId);
      if (!list) return;
      const moved = moveById(list.items, itemId, toIndex);
      if (moved.every((item, i) => item.id === list.items[i]?.id)) return;
      queue.schedule({
        id: opId("move-item"),
        kind: "move-item",
        checklistId,
        itemId,
        apply: (lists) =>
          mapList(lists, checklistId, (entry) => ({
            ...entry,
            items: moveById(entry.items, itemId, toIndex),
          })),
        send: (version) =>
          api.moveChecklistItem({
            itemId,
            expectedVersion: version ?? 0,
            ...placementOf(moved, itemId),
          }),
      });
    },
    [api, checklists, queue],
  );

  /**
   * Turns an item into a sub-item: the sub-item is made first, then the item
   * leaves the list. When making it fails nothing changes and this rejects;
   * when only the removal fails, the sub-item exists and the item stays listed
   * with a "convert-cleanup" failure the user can retry.
   */
  const convertItem = useCallback(
    async (checklistId: string, itemId: string) => {
      const title = checklists
        .find((list) => list.id === checklistId)
        ?.items.find((item) => item.id === itemId)?.title;
      if (!title) return;
      await itemExtras.addChildItem(cardId, title);
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(
          workspaceId,
          "work-management",
          "item-children",
          cardId,
        ),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
      deleteItem(checklistId, itemId, "convert-cleanup");
    },
    [
      boardId,
      cardId,
      checklists,
      deleteItem,
      itemExtras,
      queryClient,
      workspaceId,
    ],
  );

  return {
    addList,
    renameList,
    deleteList,
    moveList,
    addItem,
    renameItem,
    toggleItem,
    setItemAssignee,
    setItemDue,
    deleteItem,
    moveItem,
    convertItem,
    /** Takes the delete that is waiting back. */
    undo: queue.undo.bind(queue),
    retry: queue.retry.bind(queue),
    discard: queue.discard.bind(queue),
    dismissConflict: queue.dismissConflict.bind(queue),
    failure: state.failure,
    conflict: state.conflict,
    pendingDelete: state.pendingDelete,
    busy: state.busy,
  };
}

export type ChecklistWrites = ReturnType<typeof useChecklistWrites>;
