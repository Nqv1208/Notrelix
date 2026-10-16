import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { markInitialsFor } from "../../core/model/navigation";
import type { MarkToneId } from "../../core/model/navigation";
import type { WorkPriority, WorkStatusBucket } from "../home/home-models";
import { MARK_TONE_CLASS } from "../shell/shell-models";
import { FOCUS } from "../spaces/space-page-parts";
import { ReportGlyph } from "./report-glyph";
import { personInitials, type ReportPersonDto } from "./reports-models";

/** Panel surface of the mockup's `--panel`. */
export const PANEL = "bg-popover dark:bg-card";

/** A 32 px icon button (Final-Reports `.icon-btn`). */
export const ICON_BTN = cn(
  "grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
  FOCUS,
);

/** Text link button (Final-Reports `.lnk`). */
export const LINK_BTN = cn(
  "inline-flex cursor-pointer items-center gap-1 border-0 bg-transparent px-0.5 text-crumb leading-[1.45] font-semibold text-brand-ink hover:underline",
  FOCUS,
);

export const STATUS_BG: Readonly<Record<WorkStatusBucket, string>> = {
  todo: "bg-work-status-todo",
  doing: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
};

/** A person's initials on a mark tone (Final-Reports `.av`). */
export function PersonMark({
  person,
  className,
}: {
  person: ReportPersonDto;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-[26px] shrink-0 place-items-center rounded-full text-mark-sm font-bold",
        MARK_TONE_CLASS[person.tone],
        className,
      )}
    >
      {personInitials(person.name)}
    </span>
  );
}

/** The dashed "?" mark of nobody (Final-Reports `.av.none`). */
export function NoneMark({ className }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "box-border grid size-[22px] shrink-0 place-items-center rounded-full border-[1.5px] border-dashed border-line-strong bg-transparent text-avatar-sm font-bold text-meta-foreground",
        className,
      )}
    >
      ?
    </span>
  );
}

/** The department mark: 20 px, or 16 px small (Final-Reports `.dm`). */
export function ScopeMark({
  label,
  tone,
  small = false,
  className,
}: {
  label: string;
  tone: MarkToneId;
  small?: boolean;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center font-bold",
        small
          ? "size-4 rounded-[5px] text-mark-2xs"
          : "size-5 rounded-[6px] text-mark-xs",
        MARK_TONE_CLASS[tone],
        className,
      )}
    >
      {markInitialsFor(label)}
    </span>
  );
}

/** The department chip beside a report title (Final-Reports `.chip`). */
export function ScopeChip({
  label,
  tone,
  className,
  ...rest
}: {
  label: string;
  tone: MarkToneId;
  className?: string;
  "data-f"?: string;
}) {
  return (
    <span
      {...rest}
      className={cn(
        "inline-flex h-[26px] items-center gap-[5px] rounded-[6px] border border-line bg-popover px-2 text-caption tracking-normal leading-[1.45] font-medium text-muted-foreground dark:bg-card",
        className,
      )}
    >
      <ScopeMark label={label} tone={tone} small />
      {label}
    </span>
  );
}

export function StatusDot({
  bucket,
  className,
}: {
  bucket: WorkStatusBucket;
  className?: string;
}) {
  return (
    <i
      aria-hidden="true"
      className={cn(
        "inline-block size-[7px] shrink-0 rounded-full not-italic",
        STATUS_BG[bucket],
        className,
      )}
    />
  );
}

const FLAG_TONE: Readonly<Record<WorkPriority, string>> = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-meta-foreground",
  low: "text-meta-foreground",
};

/** The priority flag (Final-Reports `.pf-u/.pf-h/.pf-m`). */
export function PriorityFlag({
  priority,
  className,
}: {
  priority: WorkPriority;
  className?: string;
}) {
  return (
    <ReportGlyph
      name={priority === "low" ? "pole" : "flag"}
      className={cn("size-[13px]", FLAG_TONE[priority], className)}
    />
  );
}

/** A keycap (Final-Reports `.kbd`). */
export function Kbd({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground">
      {children}
    </span>
  );
}
