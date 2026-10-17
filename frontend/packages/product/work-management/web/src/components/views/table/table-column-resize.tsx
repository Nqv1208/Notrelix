import { useRef, useState, type KeyboardEvent, type PointerEvent } from "react";
import { cn } from "@notrelix/ui-web";
import {
  clampColumnWidth,
  TABLE_MAX_COLUMN_WIDTH,
  TABLE_MIN_COLUMN_WIDTH,
  TABLE_RESIZE_LARGE_STEP,
  TABLE_RESIZE_STEP,
} from "./table-columns";

export interface TableColumnResizeHandleProps {
  /** Accessible name, e.g. "Đổi độ rộng cột Tên công việc". */
  label: string;
  /** Current width in px. */
  width: number;
  /** Shows the width while the pointer drags; `null` ends the preview. */
  onPreview: (width: number | null) => void;
  /** The width the person settled on (pointer released or key pressed). */
  onCommit: (width: number) => void;
  /** Back to the design width (double-click, Home). */
  onReset: () => void;
}

/**
 * The drag handle on the right edge of a header cell. The pointer resizes the
 * column live and saves the width on release; double-click or Home resets it;
 * Left and Right Arrow resize by 8 px (32 px with Shift) once the handle has
 * the focus. It is a focusable separator, so assistive technology announces
 * the width.
 */
export function TableColumnResizeHandle({
  label,
  width,
  onPreview,
  onCommit,
  onReset,
}: TableColumnResizeHandleProps) {
  const drag = useRef<{
    pointerId: number;
    startX: number;
    startWidth: number;
    last: number;
  } | null>(null);
  const [dragging, setDragging] = useState(false);

  const onPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) return;
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    drag.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startWidth: width,
      last: width,
    };
    setDragging(true);
  };
  const onPointerMove = (event: PointerEvent<HTMLDivElement>) => {
    const state = drag.current;
    if (!state || state.pointerId !== event.pointerId) return;
    state.last = clampColumnWidth(
      state.startWidth + event.clientX - state.startX,
    );
    onPreview(state.last);
  };
  const finish = (event: PointerEvent<HTMLDivElement>, commit: boolean) => {
    const state = drag.current;
    if (!state || state.pointerId !== event.pointerId) return;
    drag.current = null;
    setDragging(false);
    onPreview(null);
    if (commit && state.last !== state.startWidth) onCommit(state.last);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = event.shiftKey ? TABLE_RESIZE_LARGE_STEP : TABLE_RESIZE_STEP;
    if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
      event.preventDefault();
      event.stopPropagation();
      const next = clampColumnWidth(
        width + (event.key === "ArrowRight" ? step : -step),
      );
      if (next !== width) onCommit(next);
    } else if (event.key === "Home") {
      event.preventDefault();
      event.stopPropagation();
      onReset();
    }
  };

  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label={label}
      aria-valuemin={TABLE_MIN_COLUMN_WIDTH}
      aria-valuemax={TABLE_MAX_COLUMN_WIDTH}
      aria-valuenow={width}
      aria-valuetext={`${width} px`}
      tabIndex={0}
      data-column-resize=""
      data-dragging={dragging ? "true" : undefined}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={(event) => finish(event, true)}
      onPointerCancel={(event) => finish(event, false)}
      onDoubleClick={onReset}
      onKeyDown={onKeyDown}
      className={cn(
        "absolute inset-y-0 right-0 z-1 w-2 cursor-col-resize touch-none outline-none",
        "after:absolute after:inset-y-1.5 after:right-0 after:w-0.5 after:rounded-full after:bg-transparent after:transition-colors after:duration-150 after:content-['']",
        "hover:after:bg-line-strong focus-visible:after:bg-focus-ring data-[dragging=true]:after:bg-primary",
      )}
    />
  );
}
