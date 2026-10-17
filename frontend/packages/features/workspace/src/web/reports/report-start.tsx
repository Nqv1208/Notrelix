import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { BTN, FOCUS } from "../spaces/space-page-parts";
import { ReportGlyph } from "./report-glyph";
import type { ReportLayoutId } from "./reports-models";
import type { ReportsCopy } from "./reports.copy";

const CELL = "flex items-end gap-0.5 rounded bg-state-hover-strong p-[3px]";
const BAR = "block w-[5px] rounded-[1px] bg-line-strong";
const LINE = "block h-1 w-[70%] self-center rounded-sm bg-line-strong";

/** The thumbnails of the three starting layouts (Final-Reports `.wf`). */
function Wireframe({ layout }: { layout: ReportLayoutId }) {
  const base =
    "box-border grid h-24 gap-1 rounded-[8px] border border-line bg-surface-canvas p-1.5";
  if (layout === "project")
    return (
      <span
        aria-hidden="true"
        className={cn(base, "grid-cols-[2fr_1fr] grid-rows-[1fr_2fr]")}
      >
        <span className={CELL}>
          <em className={LINE} />
        </span>
        <span
          className={cn(
            CELL,
            "row-span-2 flex-col items-stretch justify-start gap-1 p-[5px]",
          )}
        >
          <em className={cn(LINE, "w-[90%]")} />
          <em className={cn(LINE, "w-[80%]")} />
          <em className={cn(LINE, "w-[85%]")} />
        </span>
        <span className={CELL}>
          <i className={cn(BAR, "h-3.5")} />
          <i className={cn(BAR, "h-5")} />
          <i className={cn(BAR, "h-[9px]")} />
          <i className={cn(BAR, "h-1")} />
        </span>
      </span>
    );
  if (layout === "team")
    return (
      <span aria-hidden="true" className={cn(base, "grid-cols-2")}>
        <span className={cn(CELL, "flex-col items-start justify-center gap-1")}>
          <em className={cn(LINE, "w-[90%] self-start")} />
          <em className={cn(LINE, "w-[60%] self-start")} />
          <em className={cn(LINE, "w-[75%] self-start")} />
          <em className={cn(LINE, "w-[40%] self-start")} />
        </span>
        <span className={cn(CELL, "flex-col items-start justify-center gap-1")}>
          <em className={cn(LINE, "w-1/2 self-start")} />
          <em className={cn(LINE, "w-[80%] self-start")} />
          <em className={cn(LINE, "w-[30%] self-start")} />
        </span>
      </span>
    );
  return (
    <span aria-hidden="true" className={cn(base, "grid-rows-[1fr_2fr]")}>
      <span className={CELL}>
        <em className={cn(LINE, "w-[45%]")} />
      </span>
      <span
        className={cn(
          CELL,
          "flex-col items-stretch justify-start gap-[5px] p-1.5",
        )}
      >
        <em className={cn(LINE, "w-[95%]")} />
        <em className={cn(LINE, "w-[95%]")} />
        <em className={cn(LINE, "w-[95%]")} />
      </span>
    </span>
  );
}

const LAYOUTS: readonly ReportLayoutId[] = ["project", "team", "deadline"];

export interface ReportStartProps {
  readonly copy: ReportsCopy;
  readonly applying: ReportLayoutId | null;
  readonly failed: boolean;
  readonly onApplyLayout: (layout: ReportLayoutId) => void;
  readonly onAddWidget: () => void;
  /** A viewer who may not edit sees the starting layouts but cannot use them. */
  readonly canEdit: boolean;
}

/** The start of an empty report (Final-Reports `trong`). */
export function ReportStart({
  copy,
  applying,
  failed,
  onApplyLayout,
  onAddWidget,
  canEdit,
}: ReportStartProps) {
  const c = copy.start;
  return (
    <div className="flex min-h-0 flex-1 flex-col overflow-auto px-7 pt-5 pb-7">
      <div className="flex max-w-[880px] flex-col gap-3">
        <h2 className="m-0 text-subheading tracking-normal leading-[1.45] font-[650]">
          {c.title}
        </h2>
        <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
          {c.intro}
        </p>
        {LAYOUTS.map((layout) => (
          <div
            key={layout}
            className="grid grid-cols-[200px_minmax(0,1fr)_auto] items-center gap-[18px] rounded-[12px] border border-line bg-popover p-3.5 hover:border-line-strong dark:bg-card"
          >
            <Wireframe layout={layout} />
            <span>
              <b className="block text-nav leading-[1.45] font-semibold">
                {c.layouts[layout].title}
              </b>
              <small className="mt-0.5 block text-crumb leading-[1.5] text-meta-foreground">
                {c.layouts[layout].body}
              </small>
            </span>
            <button
              type="button"
              disabled={!canEdit || applying !== null}
              aria-busy={applying === layout ? true : undefined}
              onClick={() => onApplyLayout(layout)}
              className={cn(BTN, "disabled:cursor-default disabled:opacity-60")}
            >
              {c.use}
            </button>
          </div>
        ))}
        <button
          type="button"
          disabled={!canEdit}
          onClick={onAddWidget}
          className={cn(
            "mt-1 flex min-h-16 w-full cursor-pointer items-center justify-center gap-2 rounded-[12px] border-[1.5px] border-dashed border-line-strong bg-popover text-nav leading-[1.45] font-medium text-muted-foreground hover:border-primary hover:text-brand-ink disabled:cursor-default disabled:opacity-60 dark:bg-card",
            FOCUS,
          )}
        >
          <ReportGlyph name="plus" />
          {c.each}
        </button>
        {failed ? (
          <p role="alert" className="m-0 text-crumb text-danger-text">
            {c.failed}
          </p>
        ) : null}
        <p className="m-0 text-compact leading-[1.55] text-meta-foreground">
          {c.note}
        </p>
      </div>
    </div>
  );
}
