import { useEffect, useRef, useState } from "react";
import { Check } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useCardPopup, useCanEdit } from "./card-popup-context";

/** The complete circle and the inline title. */
export function PopupTitleRow() {
  const { card, copy, callbacks, mode, saves, track } = useCardPopup();
  const t = copy.shell;
  const canEdit = useCanEdit();
  const done = Boolean(card.completedAt) || card.statusCategory === "done";
  const [draft, setDraft] = useState(card.title);
  const [touched, setTouched] = useState(false);
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => setDraft(card.title), [card.title]);
  // A card opened from "Thêm chi tiết" or a blank draft puts the cursor on the name.
  useEffect(() => {
    if (mode === "create") ref.current?.focus();
  }, [mode]);

  const commit = () => {
    const next = draft.trim();
    if (mode === "create") {
      callbacks.onRenameTitle(card.id, { title: next });
      return;
    }
    if (next === "" || next === card.title) {
      setDraft(card.title);
      return;
    }
    track("title", async () =>
      callbacks.onRenameTitle(card.id, { title: next }),
    );
  };
  const invalid = touched && draft.trim() === "";
  const save = saves.title;

  return (
    <div className="box-border flex h-[60px] items-start gap-3 pt-4 pr-6 pl-6">
      {mode === "view" ? (
        <button
          type="button"
          role="checkbox"
          aria-checked={done}
          aria-label={done ? t.reopen : t.complete}
          title={done ? t.reopen : t.complete}
          disabled={
            !canEdit || !(done ? callbacks.onReopen : callbacks.onComplete)
          }
          onClick={() =>
            done
              ? callbacks.onReopen?.(card.id)
              : callbacks.onComplete?.(card.id)
          }
          className={cn(
            "mt-2.5 box-border grid size-6 shrink-0 place-items-center rounded-full border-2 border-line-strong bg-transparent p-0 text-primary-foreground hover:border-work-status-done focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:cursor-default",
            done && "border-work-status-done bg-work-status-done",
          )}
        >
          {done ? (
            <Check aria-hidden="true" className="size-3.5" strokeWidth={3} />
          ) : null}
        </button>
      ) : (
        <span
          aria-hidden="true"
          className="mt-2.5 box-border size-6 shrink-0 rounded-full border-2 border-line-strong"
        />
      )}
      <input
        ref={ref}
        type="text"
        value={draft}
        readOnly={!canEdit}
        aria-label={t.titleLabel}
        aria-invalid={invalid || undefined}
        placeholder={t.titlePlaceholder}
        title={save?.status === "saving" ? t.saving : undefined}
        onChange={(event) => {
          setDraft(event.target.value);
          if (mode === "create")
            callbacks.onRenameTitle(card.id, { title: event.target.value });
        }}
        onBlur={() => {
          setTouched(true);
          if (canEdit) commit();
        }}
        onKeyDown={(event) => {
          if (event.key === "Enter") event.currentTarget.blur();
          if (event.key === "Escape" && draft !== card.title) {
            event.stopPropagation();
            setDraft(card.title);
          }
        }}
        className={cn(
          "-ml-2 box-border h-11 min-w-0 flex-1 rounded-lg border-0 bg-transparent px-2 text-[22px] font-[650] tracking-[-0.01em] text-foreground placeholder:font-medium placeholder:text-meta-foreground hover:bg-state-hover-strong focus:bg-popover focus:ring-2 focus:ring-primary focus:outline-none",
          invalid && "bg-popover ring-2 ring-danger",
        )}
      />
    </div>
  );
}
