import { useId, useState } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogTitle,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import type { CardDetail } from "@notrelix/work-management-core";
import {
  useBoardChecklists,
  useClearItemLinks,
  useItemLinks,
} from "@notrelix/work-management-state";
import { SmallBtn } from "../popup-ui";
import { pluralCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

export const CHECKLIST_COPY_NONE = "none";

/** The select button of the add-checklist form: `.in` (36 px, 8 px radius) with its chevron. */
const TRIGGER =
  "box-border h-11 w-full justify-start data-[size=default]:h-11 md:data-[size=default]:h-9 md:h-9 gap-2 rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] text-foreground [&>svg:last-child]:ml-auto [&>svg:last-child]:size-[13px]";

/** `.ddl` and `.opt`: a 10 px radius list with 6 px around 44 px rows (the viewport keeps the trigger's width). */
const LIST = "rounded-[10px] p-0.5 [&_[data-radix-select-viewport]]:min-w-0";
const OPTION =
  "min-h-11 rounded-[8px] px-2.5 py-0 text-[14px] [&>span:first-child]:right-2.5";

/**
 * The "Chép mục từ…" choice of the add-checklist form: another checklist of the same board whose items
 * go into the new one. It only picks: the form's button creates the checklist (and copies the items).
 */
export function ChecklistCopyFrom({
  card,
  value,
  onChange,
  error,
  disabled = false,
}: {
  card: CardDetail;
  value: string;
  onChange: (next: string) => void;
  error?: boolean;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const id = useId();
  const [open, setOpen] = useState(false);
  const checklists = useBoardChecklists(card.boardId, card.workspaceId, open);
  // This card's own checklists first, then the board's in its order.
  const entries = [...(checklists.data ?? [])].sort(
    (a, b) => Number(b.itemId === card.id) - Number(a.itemId === card.id),
  );
  const picked = entries.find((entry) => entry.id === value);
  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor={id}
        className="text-[12px] leading-[18px] font-semibold text-muted-foreground"
      >
        {copy.checklist.copyFrom}
      </label>
      <Select
        value={value}
        disabled={disabled}
        onOpenChange={setOpen}
        onValueChange={onChange}
      >
        <SelectTrigger id={id} className={TRIGGER}>
          <SelectValue placeholder={copy.checklist.none}>
            {picked ? picked.title : copy.checklist.none}
          </SelectValue>
        </SelectTrigger>
        <SelectContent className={LIST}>
          <SelectItem value={CHECKLIST_COPY_NONE} className={OPTION}>
            {copy.checklist.none}
          </SelectItem>
          {entries.map((entry) => (
            <SelectItem key={entry.id} value={entry.id} className={OPTION}>
              <span className="flex flex-col leading-[1.3]">
                <span>{entry.title}</span>
                <small className="text-[12.5px] text-meta-foreground">
                  {pluralCapabilityCopy(
                    copy.checklist.entry_one,
                    copy.checklist.entry_other,
                    entry.itemCount,
                    {
                      item:
                        entry.itemId === card.id
                          ? copy.checklist.thisCard
                          : (entry.itemKey ?? entry.itemName),
                    },
                  )}
                </small>
              </span>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {copy.checklist.failed}
        </p>
      ) : null}
    </div>
  );
}

/** "Gỡ tất cả liên kết": shows only when the card has links, asks once, then removes every link of the card; the linked cards stay. */
export function ClearAllLinks({
  card,
  disabled = false,
}: {
  card: CardDetail;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const links = useItemLinks(card.id, card.workspaceId, card.boardId);
  const linkCount = links.data?.length ?? 0;
  const write = useClearItemLinks({
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  });
  if (linkCount === 0) return null;
  return (
    <>
      <SmallBtn
        disabled={disabled || write.isPending}
        onClick={() => setOpen(true)}
      >
        {copy.links.clearAll}
      </SmallBtn>
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {error}
        </p>
      ) : null}
      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent>
          <AlertDialogTitle>{copy.links.clearAll}</AlertDialogTitle>
          <AlertDialogDescription>
            {pluralCapabilityCopy(
              copy.links.confirm_one,
              copy.links.confirm_other,
              linkCount,
            )}
          </AlertDialogDescription>
          <AlertDialogFooter>
            <AlertDialogCancel>{copy.links.cancel}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setError(null);
                write.mutate(undefined, {
                  onError: () => setError(copy.links.failed),
                });
              }}
            >
              {copy.links.confirmAction}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
