import { useState } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogTitle,
  Button,
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
  useCopyChecklist,
  useItemLinks,
} from "@notrelix/work-management-state";
import { fillCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

const NONE = "none";

/**
 * The "Chép mục từ…" choice of the add-checklist form: another checklist of the same board whose items
 * are copied into the new one, in one change. Pick one and press the button: nothing is created until then.
 */
export function ChecklistCopyFrom({
  card,
  disabled = false,
  onCopied,
}: {
  card: CardDetail;
  disabled?: boolean;
  onCopied?: (checklistId: string) => void;
}) {
  const copy = useCapabilitiesCopy();
  const [open, setOpen] = useState(false);
  const [source, setSource] = useState(NONE);
  const [error, setError] = useState<string | null>(null);
  const checklists = useBoardChecklists(card.boardId, card.workspaceId, open);
  const write = useCopyChecklist({
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  });
  return (
    <div className="flex items-center gap-2">
      <Select
        value={source}
        disabled={disabled || write.isPending}
        onOpenChange={setOpen}
        onValueChange={(value) => {
          setSource(value);
          if (value === NONE) return;
          setError(null);
          write.mutate(
            { sourceChecklistId: value },
            {
              onSuccess: (id) => {
                setSource(NONE);
                onCopied?.(id);
              },
              onError: () => setError(copy.checklist.failed),
            },
          );
        }}
      >
        <SelectTrigger aria-label={copy.checklist.copyFrom} className="w-full">
          <SelectValue placeholder={copy.checklist.copyFrom} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={NONE}>{copy.checklist.none}</SelectItem>
          {(checklists.data ?? []).map((entry) => (
            <SelectItem key={entry.id} value={entry.id}>
              {fillCapabilityCopy(copy.checklist.entry, {
                title: entry.title,
                item: entry.itemKey ?? entry.itemName,
                count: entry.itemCount,
              })}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {error}
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
      <Button
        type="button"
        variant="ghost"
        size="sm"
        disabled={disabled || write.isPending}
        onClick={() => setOpen(true)}
      >
        {copy.links.clearAll}
      </Button>
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {error}
        </p>
      ) : null}
      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent>
          <AlertDialogTitle>{copy.links.clearAll}</AlertDialogTitle>
          <AlertDialogDescription>
            {fillCapabilityCopy(copy.links.confirm, { count: linkCount })}
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
