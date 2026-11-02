import { useState } from "react";
import { Check, ChevronDown, Loader2, MoreHorizontal, X } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import { ItemActionsMenuItems } from "../item-actions/item-actions-menu";
import { useItemActionsCopy } from "../item-actions/use-item-actions-copy";
import {
  STATUS_DOT_CLASS,
  describeItemStatus,
  type ItemStatusCategory,
} from "../card-detail/item-field-editors";
import { approvalCandidates } from "./approval/approval-model";
import { MOVE_SAVE_KEY, useCardPopup } from "./card-popup-context";
import { fillPopupCopy } from "./card-popup.copy";
import { IconBtn, PanelPopover } from "./popup-ui";

const GROUP_BTN =
  "relative inline-flex h-9 items-center gap-2 rounded-[8px] border-0 bg-state-hover-strong py-0 pr-2.5 pl-3 text-[14px] font-semibold text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

/** The dot of a column: the Status category's tint, else the option's own colour. */
function LaneDot({ lane }: { lane: Lane }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "size-[9px] shrink-0 rounded-full",
        lane.category ? STATUS_DOT_CLASS[lane.category] : "bg-meta-foreground",
      )}
      style={
        !lane.category && lane.color
          ? { backgroundColor: lane.color }
          : undefined
      }
    />
  );
}

const NO_LANE: Lane = { id: "", title: "", category: null };

interface Lane {
  readonly id: string;
  readonly title: string;
  readonly category: ItemStatusCategory | null;
  readonly color?: string;
}

/**
 * The column of the card, and moving it is one pick that changes what the board groups by: the Status
 * (a drag between lanes writes the same), the Select field the view is grouped by, or, on a board with
 * neither, the card's own board group ("Move to group"). It is always shown; without the right to move
 * it reads as a label and the notice under the header says why.
 */
function GroupPicker() {
  const {
    card,
    groups,
    groupField,
    copy,
    mode,
    actions,
    callbacks,
    readOnly,
    capabilities,
    fields,
    saves,
    track,
  } = useCardPopup();
  const [open, setOpen] = useState(false);
  const t = copy.shell;
  const laneField = groupField ?? fields.status;
  const isStatus =
    laneField !== undefined && laneField.id === fields.status?.id;
  const lanes: readonly Lane[] = laneField
    ? laneField.options.map((option) => ({
        id: option.id,
        title: option.label,
        category: option.category ?? null,
        color: option.color,
      }))
    : groups.map((group) => ({ ...group, category: null }));
  const stored = laneField ? card.fieldValues[laneField.id] : undefined;
  const currentId = laneField
    ? isStatus
      ? (describeItemStatus(card, laneField).option?.id ?? null)
      : typeof stored === "string"
        ? stored
        : null
    : card.listId;
  const current = lanes.find((lane) => lane.id === currentId);
  const saving = saves[MOVE_SAVE_KEY]?.status === "saving";
  const canMove =
    readOnly === null && (mode === "create" || capabilities.canMoveCard);
  const move = (laneId: string) => {
    setOpen(false);
    if (laneId === currentId || saving) return;
    if (laneField)
      track(MOVE_SAVE_KEY, async () =>
        callbacks.onUpdateFieldValue({
          cardId: card.id,
          fieldDefinitionId: laneField.id,
          value: laneId,
        }),
      );
    else if (mode === "create") actions.moveToGroup?.(laneId);
    else
      track(MOVE_SAVE_KEY, async () =>
        callbacks.onMoveToGroup?.(card.id, laneId),
      );
  };
  const label = (
    <>
      <LaneDot lane={current ?? NO_LANE} />
      {current?.title ??
        (laneField
          ? isStatus
            ? t.noLane
            : fillPopupCopy(t.noLaneField, { field: laneField.name })
          : "")}
    </>
  );
  if (!canMove) return <span className={cn(GROUP_BTN, "pr-3")}>{label}</span>;
  const picker = laneField
    ? { hint: t.groupPicker, title: t.groupMenuTitle, note: t.groupNote }
    : {
        hint: t.groupPickerGroup,
        title: t.groupMenuTitleGroup,
        note: t.groupNoteGroup,
      };
  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      label={picker.title}
      width={280}
      trigger={
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-haspopup="listbox"
            aria-expanded={open}
            aria-busy={saving}
            disabled={saving}
            title={mode === "create" ? t.groupPickerCreate : picker.hint}
            className={cn(
              GROUP_BTN,
              open && "ring-2 ring-primary",
              "disabled:cursor-progress",
            )}
          >
            {label}
            {saving ? (
              <>
                <Loader2
                  aria-hidden="true"
                  className="size-[13px] animate-spin text-meta-foreground motion-reduce:animate-none"
                />
                <span className="sr-only">{t.saving}</span>
              </>
            ) : (
              <ChevronDown
                aria-hidden="true"
                className="size-[13px] text-meta-foreground"
              />
            )}
          </button>
        </PopoverTrigger>
      }
    >
      <div className="mb-2 flex h-8 items-center justify-center">
        <b className="text-[14px] font-[650]">{picker.title}</b>
      </div>
      <div role="listbox" aria-label={picker.title} className="flex flex-col">
        {lanes.map((lane) => {
          const selected = lane.id === currentId;
          return (
            <button
              key={lane.id}
              type="button"
              role="option"
              aria-selected={selected}
              onClick={() => move(lane.id)}
              className={cn(
                "flex min-h-10 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] hover:bg-state-hover-strong",
                selected &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              <LaneDot lane={lane} />
              {lane.title}
              {selected ? (
                <Check aria-hidden="true" className="ml-auto size-4" />
              ) : null}
            </button>
          );
        })}
      </div>
      <p className="mt-2.5 mb-0 px-0.5 text-[12.5px] leading-[1.45] text-meta-foreground">
        {picker.note}
      </p>
    </PanelPopover>
  );
}

/** The header bar: column picker, key, the cover slot, "⋯" and close. */
export function PopupHeader({ onClose }: { onClose: () => void }) {
  const {
    card,
    board,
    mode,
    copy,
    capabilities,
    callbacks,
    slots,
    readOnly,
    data,
  } = useCardPopup();
  const actionsCopy = useItemActionsCopy();
  const t = copy.shell;
  const frozen = readOnly !== null;
  const approvers = approvalCandidates(board, data.people, data.viewerUserId);
  const canAsk =
    !frozen &&
    capabilities.canEditFields &&
    callbacks.onRequestApproval !== undefined &&
    approvers.length > 0;
  return (
    <div className="relative z-[6] flex h-14 flex-none items-center gap-1.5 border-b border-line bg-popover pr-1 pl-3 md:pr-3 md:pl-4">
      {mode === "create" ? (
        <span className="pr-2 text-[12.5px] text-meta-foreground">
          {t.groupPickerCreate}
        </span>
      ) : null}
      <GroupPicker />
      {mode === "view" && card.itemKey ? (
        <span className="ml-0.5 font-mono text-[12px] text-meta-foreground">
          {card.itemKey}
        </span>
      ) : null}
      <span className="flex-1" />
      {slots.cover}
      {mode === "view" ? (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <IconBtn aria-label={t.more} aria-haspopup="menu">
              <MoreHorizontal aria-hidden="true" className="size-4" />
            </IconBtn>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-72">
            <ItemActionsMenuItems
              copy={actionsCopy}
              item={{
                itemId: card.id,
                itemKey: card.itemKey,
                archived: card.isArchived,
                completed: Boolean(card.completedAt),
              }}
              can={{
                edit: capabilities.canEditFields && !frozen,
                create: capabilities.canCreateCard,
                move: capabilities.canMoveCard,
                remove: capabilities.canDelete,
              }}
              on={{
                copyLink: callbacks.onCopyLink,
                remind: frozen ? undefined : callbacks.onRemind,
                complete: callbacks.onComplete,
                reopen: callbacks.onReopen,
                duplicate: callbacks.onDuplicate,
                moveBoard: callbacks.onMoveToBoard,
                archive: callbacks.onArchive,
                unarchive: callbacks.onUnarchive,
                remove: callbacks.onDelete,
              }}
            />
            {canAsk ? (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuSub>
                  <DropdownMenuSubTrigger>
                    {copy.approval.request}
                  </DropdownMenuSubTrigger>
                  <DropdownMenuSubContent
                    aria-label={copy.approval.searchLabel}
                    className="max-h-72 overflow-y-auto"
                  >
                    {approvers.map((person) => (
                      <DropdownMenuItem
                        key={person.id}
                        onSelect={() =>
                          callbacks.onRequestApproval?.(person.id)
                        }
                      >
                        {person.label}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuSubContent>
                </DropdownMenuSub>
              </>
            ) : null}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null}
      <IconBtn aria-label={t.close} onClick={onClose}>
        <X aria-hidden="true" className="size-4" />
      </IconBtn>
      <span className="sr-only">{board.title}</span>
    </div>
  );
}
