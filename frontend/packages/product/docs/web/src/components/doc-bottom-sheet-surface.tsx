import { useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useModalFocus } from "./doc-share-dialog-surface";

/**
 * A bottom sheet on a phone (Final-Docs-2-320-Sheet): scrim, grabber, title
 * row with a 44 px close button, then the body. Fits its content up to 85% of
 * the viewport; Escape closes and Tab stays inside.
 */
export function DocBottomSheet({
  title,
  closeLabel,
  onClose,
  className,
  children,
}: {
  title: string;
  closeLabel: string;
  onClose?: () => void;
  className?: string;
  children: ReactNode;
}) {
  const dialog = useRef<HTMLDivElement | null>(null);
  const titleId = useId();
  const onKeyDown = useModalFocus(dialog, onClose, false);
  return (
    <div
      className="fixed inset-0 z-modal bg-modal-scrim"
      onKeyDown={onKeyDown}
      onPointerDown={(event) => {
        if (event.target === event.currentTarget) onClose?.();
      }}
    >
      <div
        ref={dialog}
        role="dialog"
        aria-modal="true"
        tabIndex={-1}
        aria-labelledby={titleId}
        className={cn(
          "absolute inset-x-0 bottom-0 flex max-h-[85vh] flex-col rounded-t-[14px] bg-doc-panel pt-1.5 pb-[env(safe-area-inset-bottom)] shadow-dialog outline-none",
          className,
        )}
      >
        <div
          aria-hidden="true"
          className="mx-auto mb-0.5 h-[5px] w-9 shrink-0 rounded-[3px] bg-line-strong"
        />
        <div className="flex min-h-[52px] shrink-0 items-center gap-2 pr-2 pl-5">
          <h2 id={titleId} className="flex-1 text-[17px] font-[650]">
            {title}
          </h2>
          <button
            type="button"
            aria-label={closeLabel}
            onClick={onClose}
            className="grid size-11 shrink-0 place-items-center rounded-[10px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <X aria-hidden="true" className="size-5 stroke-[1.8]" />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

const ROW =
  "flex min-h-11 w-full items-center rounded-[8px] border-0 bg-transparent px-3 text-left text-nav text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:text-muted-foreground";

export interface DocMoreSheetItem {
  readonly id: string;
  readonly label: string;
  readonly onSelect?: () => void;
  /** Why the row is unavailable; the row stays visible. */
  readonly unavailable?: string;
}

/** The overflow sheet behind the top bar's "More": the actions the bar has no room for. */
export function DocMoreSheet({
  title,
  closeLabel,
  items,
  onClose,
}: {
  title: string;
  closeLabel: string;
  items: readonly DocMoreSheetItem[];
  onClose: () => void;
}) {
  return (
    <DocBottomSheet title={title} closeLabel={closeLabel} onClose={onClose}>
      <div className="flex flex-col gap-0.5 overflow-auto px-3 pb-3">
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            aria-disabled={item.onSelect ? undefined : true}
            title={item.onSelect ? undefined : item.unavailable}
            onClick={item.onSelect}
            className={ROW}
          >
            {item.label}
          </button>
        ))}
      </div>
    </DocBottomSheet>
  );
}
