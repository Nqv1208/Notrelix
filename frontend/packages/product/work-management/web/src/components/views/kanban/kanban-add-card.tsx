import { useEffect, useRef, useState } from "react";
import { Plus, X } from "lucide-react";
import { Button, Input } from "@notrelix/ui-web";
import { fillCopy } from "./kanban-card.copy";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

/**
 * "Thêm vào {lane}": a lane footer button that opens an inline title field.
 * Submitting hands the title to the container (which creates the item and
 * sets the lane's value) and keeps the field open for the next item; Escape
 * or Huỷ closes it and returns focus to the button.
 */
export function KanbanAddCard({
  laneName,
  copy,
  openRequest,
  onSubmit,
}: {
  laneName: string;
  copy: KanbanLaneCopy;
  /** Changing this number opens the field (the toolbar's add button). */
  openRequest?: number;
  onSubmit: (title: string) => void;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const openerRef = useRef<HTMLButtonElement>(null);
  const restoreFocus = useRef(false);
  const handledRequest = useRef(openRequest);

  useEffect(() => {
    if (openRequest === undefined || openRequest === handledRequest.current) {
      return;
    }
    handledRequest.current = openRequest;
    setIsOpen(true);
    inputRef.current?.focus();
  }, [openRequest]);

  useEffect(() => {
    if (isOpen) {
      inputRef.current?.focus();
    } else if (restoreFocus.current) {
      restoreFocus.current = false;
      openerRef.current?.focus();
    }
  }, [isOpen]);

  function submit() {
    const nextTitle = title.trim();
    if (!nextTitle) return;
    onSubmit(nextTitle);
    setTitle("");
    inputRef.current?.focus();
  }

  function cancel() {
    setTitle("");
    restoreFocus.current = true;
    setIsOpen(false);
  }

  const values = { lane: laneName };

  if (!isOpen) {
    return (
      <button
        ref={openerRef}
        type="button"
        onClick={() => setIsOpen(true)}
        className="flex h-8.5 w-full items-center gap-1.5 rounded-[9px] px-2 text-compact text-muted-foreground transition-colors hover:bg-popover hover:text-foreground dark:hover:bg-card focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <Plus aria-hidden="true" className="size-4" />
        {fillCopy(copy.addTo, values)}
      </button>
    );
  }

  return (
    <form
      className="flex flex-col gap-2 rounded-[11px] bg-popover p-2 shadow-card dark:bg-card"
      onSubmit={(event) => {
        event.preventDefault();
        submit();
      }}
    >
      <Input
        ref={inputRef}
        value={title}
        onChange={(event) => setTitle(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.stopPropagation();
            cancel();
          }
        }}
        placeholder={copy.newItemPlaceholder}
        aria-label={fillCopy(copy.newItemLabel, values)}
        className="h-8 text-label"
      />
      <div className="flex items-center gap-1.5">
        <Button type="submit" size="sm" disabled={!title.trim()}>
          {copy.addSubmit}
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={cancel}
          aria-label={copy.addCancel}
        >
          <X aria-hidden="true" className="size-4" />
        </Button>
      </div>
    </form>
  );
}
