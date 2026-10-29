import { useMemo, useRef, useState, type ReactNode } from "react";
import { toast } from "sonner";
import {
  useArchiveCard,
  useBoardLabels,
  useCreateItemWithDetails,
  type CreateItemDetail,
} from "@notrelix/work-management-state";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import { findItemFields } from "../card-detail/item-field-editors";
import { useAddGroup } from "../status-option/use-add-group";
import {
  ItemCreateDialog,
  type ItemCreateContext,
  type ItemCreateCreated,
  type ItemCreateDialogProps,
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
 * With "create another" the form stays open and offers to open or undo the
 * item just added.
 */
export function ItemCreateHost({
  open,
  onOpenChange,
  workspaceId,
  board,
  groups,
  defaultGroupId,
  hideGroup,
  defaultStatusId,
  defaultStartDay,
  defaultDueDay,
  defaultTitle,
  defaultDescription,
  defaultPriorityId,
  heading,
  intro,
  sources,
  footnote,
  submitLabel,
  successMessage,
  hideAnother,
  focusDescription,
  defaultAssigneeIds,
  currentUserId,
  laneValue,
  context,
  boardSlot,
  onCreated,
  onOpenItem,
}: {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspaceId: string;
  /** Undefined while a board chosen in `boardSlot` loads: the form cannot be submitted yet. */
  readonly board: Board | undefined;
  readonly groups: readonly BoardGroup[];
  readonly defaultGroupId: string;
  /** The groups mirror the statuses: the form shows no Group field. */
  readonly hideGroup?: boolean;
  readonly defaultStatusId: string | null;
  /** `YYYY-MM-DD` prefilled in the dates (Calendar and Timeline). */
  readonly defaultStartDay?: string;
  readonly defaultDueDay?: string;
  readonly defaultTitle?: string;
  /** A form response prefills these (see `ItemCreateDialog`). */
  readonly defaultDescription?: string;
  readonly defaultPriorityId?: string | null;
  readonly heading?: string;
  readonly intro?: ReactNode;
  readonly sources?: ItemCreateDialogProps["sources"];
  readonly footnote?: ReactNode;
  readonly submitLabel?: string;
  /** The success toast's words, when the item is not just "added". */
  readonly successMessage?: string;
  readonly hideAnother?: boolean;
  /** The name came from a quick add: the cursor starts in the description. */
  readonly focusDescription?: boolean;
  readonly defaultAssigneeIds?: readonly string[];
  readonly currentUserId?: string | null;
  /** Lane of a board not grouped by Status: written as a follow-up value. */
  readonly laneValue?: {
    readonly fieldDefinitionId: string;
    readonly value: string;
  } | null;
  readonly context?: ItemCreateContext;
  readonly boardSlot?: ReactNode;
  /** After the item exists (also when a detail could not be saved). */
  readonly onCreated?: (result: {
    readonly boardId: string;
    readonly itemId: string | undefined;
  }) => void;
  /** "Mở" on the strip shown after "create another": opens the new item. */
  readonly onOpenItem?: (itemId: string) => void;
}) {
  const copy = useItemCreateCopy();
  const create = useCreateItemWithDetails(board?.id ?? "", workspaceId);
  const archive = useArchiveCard(board?.id ?? "", workspaceId);
  const { addGroup, isPending: addingGroup } = useAddGroup(
    board?.id ?? "",
    workspaceId,
  );
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<ItemCreateCreated | null>(null);
  const nonce = useRef(0);
  const fields = board ? findItemFields(board) : null;
  // The board's own label catalogue, not only the labels of the cards loaded.
  const catalogue = useBoardLabels(
    board?.id ?? "",
    workspaceId,
    open && Boolean(board),
  );
  const labels = useMemo<ItemCreateLabel[]>(
    () =>
      (catalogue.data ?? []).map((label) => ({
        id: label.id,
        name: label.name ?? label.color,
        color: label.color,
      })),
    [catalogue.data],
  );

  const submit = (draft: ItemCreateDraft) => {
    if (!board) {
      setError(copy.chooseBoard);
      return;
    }
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
          fields?.priority && draft.priorityOptionId
            ? {
                fieldDefinitionId: fields.priority.id,
                value: draft.priorityOptionId,
              }
            : null,
        assigneeIds: draft.assigneeIds,
        labelIds: draft.labelIds,
        laneValue: laneValue ?? null,
      },
      {
        onSuccess: ({ failed, itemId }) => {
          if (board) onCreated?.({ boardId: board.id, itemId });
          if (failed.length > 0) {
            toast.warning(
              copy.createdWithout.replace(
                "{{details}}",
                failed
                  .map((detail: CreateItemDetail) => copy.detailNames[detail])
                  .join(", "),
              ),
            );
          } else if (!draft.another) {
            toast.success(successMessage ?? copy.created);
          }
          if (draft.another) {
            nonce.current += 1;
            setCreated({
              nonce: nonce.current,
              title: draft.title,
              onOpen:
                itemId && onOpenItem ? () => onOpenItem(itemId) : undefined,
              onUndo: itemId
                ? () =>
                    archive.mutate(itemId, {
                      onSuccess: () => toast.success(copy.undone),
                      onError: () => toast.error(copy.undoFailed),
                    })
                : undefined,
            });
          } else {
            onOpenChange(false);
          }
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
      hideGroup={hideGroup}
      statuses={(fields?.status?.options ?? []).map((option) => ({
        id: option.id,
        label: option.label,
      }))}
      defaultStatusId={
        defaultStatusId ?? fields?.status?.options[0]?.id ?? null
      }
      defaultStartDay={defaultStartDay}
      defaultDueDay={defaultDueDay}
      defaultTitle={defaultTitle}
      defaultDescription={defaultDescription}
      defaultPriorityId={defaultPriorityId}
      heading={heading}
      intro={intro}
      sources={sources}
      footnote={footnote}
      submitLabel={submitLabel}
      hideAnother={hideAnother}
      focusDescription={focusDescription}
      defaultAssigneeIds={defaultAssigneeIds}
      currentUserId={currentUserId}
      context={context}
      created={created}
      boardSlot={boardSlot}
      unavailable={!board}
      onAddGroup={
        board
          ? () =>
              void addGroup({ title: copy.defaultGroupTitle }, false).catch(
                () => setError(copy.groupAddFailed),
              )
          : undefined
      }
      addGroupPending={addingGroup}
      priorities={(fields?.priority?.options ?? []).map((option) => ({
        id: option.id,
        label: option.label,
      }))}
      members={(board?.members ?? []).map((member) => ({
        userId: member.userId,
        name: member.name,
        initials: member.initials,
      }))}
      labels={labels}
      pending={create.isPending}
      error={error}
      onOpenChange={(next) => {
        if (!next) {
          setError(null);
          setCreated(null);
        }
        onOpenChange(next);
      }}
      onSubmit={submit}
    />
  );
}
