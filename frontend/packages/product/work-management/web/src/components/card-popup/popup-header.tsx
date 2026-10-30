import { useState } from "react";
import { Check, ChevronDown, MoreHorizontal, X } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import { ItemActionsMenuItems } from "../item-actions/item-actions-menu";
import { useItemActionsCopy } from "../item-actions/use-item-actions-copy";
import { STATUS_DOT_CLASS } from "../card-detail/item-field-editors";
import { useCardPopup } from "./card-popup-context";
import { IconBtn, PanelPopover } from "./popup-ui";

const GROUP_BTN =
  "relative inline-flex h-9 items-center gap-2 rounded-[8px] border-0 bg-state-hover-strong py-0 pr-2.5 pl-3 text-[14px] font-semibold text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

/** The column of the card: shows where it is, and moving it is one pick. */
function GroupPicker() {
  const {
    card,
    groups,
    copy,
    mode,
    actions,
    callbacks,
    readOnly,
    capabilities,
    fields,
  } = useCardPopup();
  const [open, setOpen] = useState(false);
  const t = copy.shell;
  const current = groups.find((group) => group.id === card.listId);
  // A column that mirrors a status shows that status's dot.
  const mirrored = fields.status?.options.find(
    (option) => option.label === current?.title,
  );
  const category = mirrored?.category ?? null;
  const dot = category ? STATUS_DOT_CLASS[category] : null;
  const canMove =
    readOnly === null && (mode === "create" || capabilities.canMoveCard);
  const move = (groupId: string) => {
    setOpen(false);
    if (groupId === card.listId) return;
    if (mode === "create") actions.moveToGroup?.(groupId);
    else
      (actions.moveToGroup ?? ((id) => callbacks.onMoveToGroup?.(card.id, id)))(
        groupId,
      );
  };
  const label = (
    <>
      <span
        aria-hidden="true"
        className={cn(
          "size-[9px] shrink-0 rounded-full",
          dot ?? "bg-meta-foreground",
        )}
      />
      {current?.title ?? ""}
    </>
  );
  if (!canMove) return <span className={cn(GROUP_BTN, "pr-3")}>{label}</span>;
  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={280}
      trigger={
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-haspopup="listbox"
            aria-expanded={open}
            title={mode === "create" ? t.groupPickerCreate : t.groupPicker}
            className={cn(GROUP_BTN, open && "ring-2 ring-primary")}
          >
            {label}
            <ChevronDown
              aria-hidden="true"
              className="size-[13px] text-meta-foreground"
            />
          </button>
        </PopoverTrigger>
      }
    >
      <div className="mb-2 flex h-8 items-center justify-center">
        <b className="text-[14px] font-[650]">{t.groupMenuTitle}</b>
      </div>
      <div
        role="listbox"
        aria-label={t.groupMenuTitle}
        className="flex flex-col"
      >
        {groups.map((group) => {
          const selected = group.id === card.listId;
          return (
            <button
              key={group.id}
              type="button"
              role="option"
              aria-selected={selected}
              onClick={() => move(group.id)}
              className={cn(
                "flex min-h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] hover:bg-state-hover-strong",
                selected &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              {group.title}
              {selected ? (
                <Check aria-hidden="true" className="ml-auto size-4" />
              ) : null}
            </button>
          );
        })}
      </div>
      <p className="mt-2.5 mb-0 px-0.5 text-[12.5px] leading-[1.45] text-meta-foreground">
        {t.groupNote}
      </p>
    </PanelPopover>
  );
}

/** The header bar: column picker, key, the cover slot, "⋯" and close. */
export function PopupHeader({ onClose }: { onClose: () => void }) {
  const { card, board, mode, copy, capabilities, callbacks, slots, readOnly } =
    useCardPopup();
  const actionsCopy = useItemActionsCopy();
  const t = copy.shell;
  const frozen = readOnly !== null;
  return (
    <div className="relative z-[6] flex h-14 flex-none items-center gap-1.5 border-b border-line bg-popover pr-1 pl-3 md:pr-3 md:pl-4">
      {mode === "create" ? (
        <span className="mr-1 text-[13.5px] text-muted-foreground">
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
