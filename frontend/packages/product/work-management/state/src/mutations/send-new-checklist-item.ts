import type { createChecklistApi } from "../api/checklist.api";

type ChecklistApi = ReturnType<typeof createChecklistApi>;

/**
 * Creates a checklist item and, when it starts with a person or a due time, gives it them.
 *
 * The create takes neither and answers nothing, so the item is found afterwards as the newest one with this
 * title in its list, and each following write names the list's version as the last write left it. If a
 * following write is refused the item exists with what was kept: failing the whole write would let a retry add
 * the item a second time, so the extras are dropped and the list shows what the server holds.
 */
export async function sendNewChecklistItem(
  api: Pick<
    ChecklistApi,
    | "createChecklistItem"
    | "getChecklists"
    | "setChecklistItemAssignee"
    | "setChecklistItemDue"
  >,
  input: {
    cardId: string;
    checklistId: string;
    text: string;
    assigneeId: string | null;
    dueAt: string | null;
    expectedVersion: number;
  },
): Promise<void> {
  const { cardId, checklistId, text, assigneeId, dueAt } = input;
  await api.createChecklistItem({
    checklistId,
    title: text,
    expectedVersion: input.expectedVersion,
  });
  if (!assigneeId && !dueAt) return;
  const newest = async () => {
    const list = (await api.getChecklists(cardId)).find(
      (entry) => entry.id === checklistId,
    );
    const item = list?.items.filter((i) => i.title === text).at(-1);
    return list && item ? { list, item } : null;
  };
  try {
    let found = await newest();
    if (found && assigneeId) {
      await api.setChecklistItemAssignee({
        itemId: found.item.id,
        assigneeUserId: assigneeId,
        expectedVersion: found.list.version ?? 0,
      });
      found = dueAt ? await newest() : found;
    }
    if (found && dueAt)
      await api.setChecklistItemDue({
        itemId: found.item.id,
        dueAt,
        expectedVersion: found.list.version ?? 0,
      });
  } catch {
    // The item exists; see above.
  }
}
