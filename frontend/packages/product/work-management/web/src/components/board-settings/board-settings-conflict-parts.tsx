import { useState, type ReactNode } from "react";
import { CircleAlert, TriangleAlert } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { BUTTON_CLASS } from "./board-settings-parts";

/** The strings of a stale-save notice and its three-column diff. The caller fills the names. */
export interface ConflictNoticeCopy {
  readonly title: string;
  readonly body: string;
  readonly showDiff: string;
  readonly hideDiff: string;
  readonly takeTheirs: string;
  readonly keepMine: string;
  readonly working: string;
  readonly diffLabel: string;
  readonly columns: {
    readonly part: string;
    readonly mine: string;
    readonly latest: string;
    readonly onSave: string;
  };
  readonly unchanged: string;
  readonly choiceMine: string;
  readonly choiceLatest: string;
  readonly diffHint: string;
  readonly keepMineAndSave: string;
}

/** One line of the diff: what the person has, what is saved now, and what the save will keep. */
export interface ConflictDiffRow {
  readonly id: string;
  readonly label: string;
  /** Null when the person did not change this part. */
  readonly mine: string | null;
  /** Null when the other person did not change this part. */
  readonly latest: string | null;
  readonly keep: "mine" | "latest";
}

const BTN_SM = "h-7 px-2.5 text-[12.5px]";

/**
 * The notice of a save refused as stale (BC-Notices): who saved first, what they
 * changed, that nothing was overwritten, and the ways out. It slides in under the
 * page title and pushes the form down, so the field being edited is never covered.
 * The diff opens in place under the notice.
 */
export function ConflictNotice({
  copy,
  busy,
  rows,
  onTakeTheirs,
  onKeepMine,
}: {
  copy: ConflictNoticeCopy;
  busy: "mine" | "theirs" | null;
  /** Absent while the latest version is still being read. */
  rows?: readonly ConflictDiffRow[];
  onTakeTheirs: () => void;
  onKeepMine: () => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div className="flex flex-col gap-3 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 motion-safe:duration-150">
      <div
        role="alert"
        className="flex flex-wrap items-start gap-2.5 rounded-[10px] bg-warning-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-warning-text"
      >
        <TriangleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
        <span className="flex min-w-[200px] flex-1 flex-col gap-px">
          <b className="font-semibold text-foreground">{copy.title}</b>
          <span>{copy.body}</span>
        </span>
        <span className="flex shrink-0 flex-wrap items-center gap-1.5">
          {rows ? (
            <button
              type="button"
              aria-expanded={open}
              onClick={() => setOpen((value) => !value)}
              className={cn(BUTTON_CLASS.outline, BTN_SM)}
            >
              {open ? copy.hideDiff : copy.showDiff}
            </button>
          ) : null}
          <button
            type="button"
            disabled={busy !== null}
            onClick={onTakeTheirs}
            className={cn(BUTTON_CLASS.outline, BTN_SM)}
          >
            {busy === "theirs" ? copy.working : copy.takeTheirs}
          </button>
          <button
            type="button"
            disabled={busy !== null}
            onClick={onKeepMine}
            className={cn(BUTTON_CLASS.primary, BTN_SM)}
          >
            {busy === "mine" ? copy.working : copy.keepMine}
          </button>
        </span>
      </div>
      {rows && open ? (
        <ConflictDiff
          copy={copy}
          rows={rows}
          busy={busy}
          onTakeTheirs={onTakeTheirs}
          onKeepMine={onKeepMine}
        />
      ) : null}
    </div>
  );
}

function ConflictDiff({
  copy,
  rows,
  busy,
  onTakeTheirs,
  onKeepMine,
}: {
  copy: ConflictNoticeCopy;
  rows: readonly ConflictDiffRow[];
  busy: "mine" | "theirs" | null;
  onTakeTheirs: () => void;
  onKeepMine: () => void;
}) {
  const grid =
    "grid grid-cols-[150px_minmax(0,1fr)_minmax(0,1fr)_140px] items-center gap-3 px-3.5 py-2.5";
  return (
    <div className="flex flex-col gap-3 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-150">
      <div
        role="table"
        aria-label={copy.diffLabel}
        className="overflow-hidden rounded-[12px] border border-line bg-popover"
      >
        <div
          role="row"
          className={cn(
            grid,
            "border-b border-line bg-table-header text-[12px] font-semibold text-meta-foreground",
          )}
        >
          <span role="columnheader">{copy.columns.part}</span>
          <span role="columnheader">{copy.columns.mine}</span>
          <span role="columnheader">{copy.columns.latest}</span>
          <span role="columnheader">{copy.columns.onSave}</span>
        </div>
        {rows.map((row) => (
          <div
            key={row.id}
            role="row"
            className={cn(
              grid,
              "min-h-14 border-t border-line text-[13.5px] leading-[1.45] first-of-type:border-t-0",
            )}
          >
            <b role="cell" className="font-semibold">
              {row.label}
            </b>
            <span
              role="cell"
              className={cn(
                "min-w-0 break-words",
                row.mine === null ? "text-meta-foreground" : "text-foreground",
              )}
            >
              {row.mine ?? copy.unchanged}
            </span>
            <span
              role="cell"
              className={cn(
                "min-w-0 break-words",
                row.latest === null
                  ? "text-meta-foreground"
                  : "text-foreground",
              )}
            >
              {row.latest ?? copy.unchanged}
            </span>
            <span role="cell">
              <span
                className={cn(
                  "inline-flex h-[22px] items-center rounded-[6px] px-2 text-[12px] font-semibold whitespace-nowrap",
                  row.keep === "mine"
                    ? "bg-label-data text-label-data-foreground"
                    : "bg-label-doc text-label-doc-foreground",
                )}
              >
                {row.keep === "mine" ? copy.choiceMine : copy.choiceLatest}
              </span>
            </span>
          </div>
        ))}
      </div>
      <div className="flex items-center justify-end gap-2">
        <span className="flex-1 text-[12.5px] text-meta-foreground">
          {copy.diffHint}
        </span>
        <button
          type="button"
          disabled={busy !== null}
          onClick={onTakeTheirs}
          className={BUTTON_CLASS.outline}
        >
          {copy.takeTheirs}
        </button>
        <button
          type="button"
          disabled={busy !== null}
          onClick={onKeepMine}
          className={BUTTON_CLASS.primary}
        >
          {busy === "mine" ? copy.working : copy.keepMineAndSave}
        </button>
      </div>
    </div>
  );
}

/** The marker under an input the other person also changed (`.dif`). */
export function ConflictFieldNote({
  children,
  tone = "warning",
}: {
  children: ReactNode;
  tone?: "warning" | "info";
}) {
  const Icon = tone === "warning" ? TriangleAlert : CircleAlert;
  return (
    <span className="flex items-start gap-1.5 text-[12.5px] leading-[1.45] font-medium text-warning-text">
      <Icon aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
      <span>{children}</span>
    </span>
  );
}

/** The outline an edited input carries while it differs from the latest version (`.cf.chg`). */
export const CHANGED_INPUT_CLASS =
  "border-warning-text shadow-[inset_0_0_0_1px_var(--warning-text)]";

/** Joins names as "a, b and c". */
export function joinNames(names: readonly string[], and: string): string {
  if (names.length <= 1) return names.join("");
  return `${names.slice(0, -1).join(", ")} ${and} ${names[names.length - 1]}`;
}

/** "10:41" of an instant, in the board's time zone. */
export function timeOf(iso: string | null, timeZone: string): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  return new Intl.DateTimeFormat("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    timeZone,
  }).format(date);
}
