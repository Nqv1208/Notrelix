import { useMemo, useState } from "react";
import { toast } from "sonner";
import {
  useCreateItemWithDetails,
  type CreateItemDetail,
} from "@notrelix/work-management-state";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import { findItemFields } from "../card-detail/item-field-editors";
import {
  ItemCreateDialog,
  type ItemCreateDraft,
  type ItemCreateLabel,
} from "./item-create-dialog";
import { useItemCreateCopy } from "./use-item-create-copy";

/** `YYYY-MM-DD` as the instant that day starts in UTC (the backend keeps UTC days). */
function dayInstant(day: string): string | undefined {
  return day === "" ? undefined : `${day}T00:00:00.000Z`;
}

/**
 * The board's "Add item" dialog with its writes: the item is created in the
 * chosen group with its description and dates, then its status, priority,
 * people and labels. A detail that cannot be saved is named and the item stays.
 */
export function ItemCreateHost({
  open,
  onOpenChange,
  workspaceId,
  board,
  groups,
  defaultGroupId,
  defaultStatusId,
  defaultStartDay,
  defaultDueDay,
}: {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspaceId: string;
  readonly board: Board;
  readonly groups: readonly BoardGroup[];
  readonly defaultGroupId: string;
  readonly defaultStatusId: string | null;
  /** `YYYY-MM-DD` prefilled in the dates (Calendar and Timeline). */
  readonly defaultStartDay?: string;
  readonly defaultDueDay?: string;
}) {
  const copy = useItemCreateCopy();
  const create = useCreateItemWithDetails(board.id, workspaceId);
  const [error, setError] = useState<string | null>(null);
  const fields = findItemFields(board);
  const labels = useMemo<ItemCreateLabel[]>(() => {
    const seen = new Map<string, ItemCreateLabel>();
    for (const group of groups) {
      for (const card of group.cards) {
        for (const label of card.labels) {
          if (!seen.has(label.id)) {
            seen.set(label.id, {
              id: label.id,
              name: label.name ?? label.color,
              color: label.color,
            });
          }
        }
      }
    }
    return [...seen.values()];
  }, [groups]);

  const submit = (draft: ItemCreateDraft) => {
    setError(null);
    create.mutate(
      {
        listId: draft.listId,
        title: draft.title,
        descriptionMd: draft.descriptionMd.trim() || undefined,
        startDate: dayInstant(draft.startDay),
        dueDate: dayInstant(draft.dueDay),
        statusOptionId: draft.statusOptionId,
        priority:
          fields.priority && draft.priorityOptionId
            ? {
                fieldDefinitionId: fields.priority.id,
                value: draft.priorityOptionId,
              }
            : null,
        assigneeIds: draft.assigneeIds,
        labelIds: draft.labelIds,
      },
      {
        onSuccess: ({ failed }) => {
          if (failed.length > 0) {
            toast.warning(
              copy.createdWithout.replace(
                "{{details}}",
                failed
                  .map((detail: CreateItemDetail) => copy.detailNames[detail])
                  .join(", "),
              ),
            );
          } else {
            toast.success(copy.created);
          }
          if (!draft.another) onOpenChange(false);
        },
        onError: () => setError(copy.failed),
      },
    );
  };

  return (
    <ItemCreateDialog
      copy={copy}
      open={open}
      groups={groups.map((group) => ({ id: group.id, label: group.title }))}
      defaultGroupId={defaultGroupId}
      statuses={(fields.status?.options ?? []).map((option) => ({
        id: option.id,
        label: option.label,
      }))}
      defaultStatusId={defaultStatusId ?? fields.status?.options[0]?.id ?? null}
      defaultStartDay={defaultStartDay}
      defaultDueDay={defaultDueDay}
      priorities={(fields.priority?.options ?? []).map((option) => ({
        id: option.id,
        label: option.label,
      }))}
      members={board.members.map((member) => ({
        userId: member.userId,
        name: member.name,
        initials: member.initials,
      }))}
      labels={labels}
      pending={create.isPending}
      error={error}
      onOpenChange={(next) => {
        if (!next) setError(null);
        onOpenChange(next);
      }}
      onSubmit={submit}
    />
  );
}
