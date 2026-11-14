import { useCallback, useRef, useState, type RefObject } from "react";
import { cn } from "@notrelix/ui-web";
import {
  SPLIT_PANE_STEP,
  clampSplitWidth,
  readSplitWidth,
  splitWidthAt,
  splitWidthStorage,
  writeSplitWidth,
} from "./table-split-width";

/**
 * The item pane's width: the drag shows at once, and the width the person leaves is saved on release
 * (and after each key step), so the next visit opens at it.
 */
export function useSplitPaneWidth() {
  const [width, setWidth] = useState(() => readSplitWidth(splitWidthStorage()));
  const commit = useCallback((next: number) => {
    const clamped = clampSplitWidth(next);
    setWidth(clamped);
    writeSplitWidth(splitWidthStorage(), clamped);
  }, []);
  return { width, preview: setWidth, commit };
}

/**
 * The divider between the table and the item pane, with the grip the artboard draws on it (a 44 px hit
 * area, an 8 px bar). Drag it, or use the arrow keys: Left widens the pane, Right narrows it.
 */
export function TableSplitDivider({
  containerRef,
  paneId,
  width,
  onPreview,
  onCommit,
  label,
}: {
  /** The element the pane and the table share: the grip reads its width to turn a pointer into a share. */
  readonly containerRef: RefObject<HTMLElement | null>;
  readonly paneId: string;
  readonly width: number;
  readonly onPreview: (width: number) => void;
  readonly onCommit: (width: number) => void;
  readonly label: string;
}) {
  const dragging = useRef<number | null>(null);
  // The share the drag reached: the release saves it, whether or not the parent has re-rendered yet.
  const last = useRef(width);
  last.current = width;

  const share = (clientX: number) => {
    const box = containerRef.current?.getBoundingClientRect();
    return box ? splitWidthAt(clientX, box) : null;
  };

  return (
    <span className="relative w-px flex-none bg-line-strong">
      <button
        type="button"
        aria-label={label}
        aria-controls={paneId}
        onPointerDown={(event) => {
          if (event.button !== 0) return;
          dragging.current = event.pointerId;
          event.currentTarget.setPointerCapture?.(event.pointerId);
        }}
        onPointerMove={(event) => {
          if (dragging.current !== event.pointerId) return;
          const next = share(event.clientX);
          if (next === null) return;
          last.current = next;
          onPreview(next);
        }}
        onPointerUp={(event) => {
          if (dragging.current !== event.pointerId) return;
          dragging.current = null;
          onCommit(last.current);
        }}
        onPointerCancel={() => {
          dragging.current = null;
        }}
        onKeyDown={(event) => {
          if (event.key === "ArrowLeft") {
            event.preventDefault();
            onCommit(width + SPLIT_PANE_STEP);
          } else if (event.key === "ArrowRight") {
            event.preventDefault();
            onCommit(width - SPLIT_PANE_STEP);
          }
        }}
        className={cn(
          "absolute top-1/2 -left-[22px] z-10 grid h-14 w-11 -translate-y-1/2 cursor-col-resize touch-none place-items-center border-0 bg-transparent p-0",
          "focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-primary",
        )}
      >
        <span
          aria-hidden="true"
          className="block h-11 w-2 rounded-[4px] border border-line-strong bg-popover"
        />
      </button>
    </span>
  );
}
