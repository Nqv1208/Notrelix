import { useEffect, useRef, useState } from "react";
import { ArrowUpRight, Plus, X } from "lucide-react";
import { Button, Input, Kbd } from "@notrelix/ui-web";
import { fillCopy } from "./kanban-card.copy";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

/**
 * "Thêm vào {lane}": a lane footer button that opens an inline title field.
 * Enter hands the title to the container (which creates the item at the end of
 * the lane) and keeps the field open for the next item; "Thêm chi tiết" and
 * Ctrl/Cmd+Enter carry what was typed into the full form; Escape or the close
 * button closes the field and returns focus to the footer button.
 */
export function KanbanAddCard({
  laneName,
  copy,
  openRequest,
  onSubmit,
  onExpand,
}: {
  laneName: string;
  copy: KanbanLaneCopy;
  /** Changing this number opens the field (the toolbar's add button). */
  openRequest?: number;
  onSubmit: (title: string) => void;
  /** Opens the full form with the typed title; absent: no "Thêm chi tiết". */
  onExpand?: (title: string) => void;
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

  function expand() {
    if (!onExpand) return;
    onExpand(title.trim());
    setTitle("");
    setIsOpen(false);
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
        className="group/add flex h-8.5 w-full items-center gap-1.5 rounded-[9px] px-2 text-compact text-muted-foreground transition-colors hover:bg-popover hover:text-foreground dark:hover:bg-card focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <Plus aria-hidden="true" className="size-4" />
        {fillCopy(copy.addTo, values)}
        <Kbd
          aria-hidden="true"
          className="ml-auto opacity-0 group-hover/add:opacity-100 group-focus-visible/add:opacity-100"
        >
          N
        </Kbd>
      </button>
    );
  }

  return (
    <form
      role="group"
      aria-label={fillCopy(copy.addTo, values)}
      className="flex flex-col gap-2 rounded-[11px] bg-popover p-2 shadow-card ring-2 ring-primary dark:bg-card"
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
            return;
          }
          if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
            event.preventDefault();
            expand();
          }
        }}
        placeholder={copy.newItemPlaceholder}
        aria-label={fillCopy(copy.newItemLabel, values)}
        className="h-8 border-0 bg-transparent px-1 text-label font-medium shadow-none focus-visible:ring-0"
      />
      <div className="flex items-center gap-1.5">
        <Button type="submit" size="sm" disabled={!title.trim()}>
          {copy.addSubmit}
        </Button>
        {onExpand ? (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={expand}
            className="text-brand-subtle-foreground hover:bg-brand-subtle"
          >
            {copy.quickDetail}
            <ArrowUpRight aria-hidden="true" className="size-3.5" />
          </Button>
        ) : null}
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="ml-auto"
          onClick={cancel}
          aria-label={copy.addCancel}
        >
          <X aria-hidden="true" className="size-4" />
        </Button>
      </div>
      <div
        aria-hidden="true"
        className="flex flex-wrap items-center gap-x-2.5 gap-y-1 px-0.5 text-micro text-meta-foreground"
      >
        <span className="inline-flex items-center gap-1">
          <Kbd>Enter</Kbd> {copy.quickHintEnter}
        </span>
        {onExpand ? (
          <span className="inline-flex items-center gap-1">
            <Kbd>⌘ Enter</Kbd> {copy.quickHintMod}
          </span>
        ) : null}
        <span className="inline-flex items-center gap-1">
          <Kbd>Esc</Kbd> {copy.quickHintEsc}
        </span>
      </div>
    </form>
  );
}
