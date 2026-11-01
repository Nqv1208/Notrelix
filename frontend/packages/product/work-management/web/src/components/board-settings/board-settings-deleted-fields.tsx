import { useState } from "react";
import { CircleAlert, CircleCheck, Info } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useSettings } from "./board-settings-context";
import { fillCopy, typeName } from "./board-settings-format";
import { BUTTON_CLASS, fieldIcon } from "./board-settings-parts";
import type {
  DeletedFieldRow,
  DeletedFieldsPanelProps,
} from "./board-settings-types";

const GRID =
  "grid grid-cols-[minmax(0,1.6fr)_150px_210px_130px_150px] items-center gap-3.5 px-4 max-[1100px]:grid-cols-[minmax(0,1.5fr)_110px_130px_130px]";
const DAY = 86_400_000;

type RowNote =
  | { kind: "name-taken" | "expired" | "forbidden" | "failed" }
  | { kind: "done"; count: number };

/** "dd/MM/yyyy" of an instant in the board's time zone. */
function fullDay(iso: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    timeZone,
  }).format(new Date(iso));
}

function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  return (
    (words[0]?.[0] ?? "") + (words.length > 1 ? words.at(-1)![0]! : "")
  ).toUpperCase();
}

function RowIcon({ type }: { type: DeletedFieldRow["type"] }) {
  const Icon = fieldIcon(type, "");
  return <Icon aria-hidden="true" className="size-4" strokeWidth={1.8} />;
}

/** The "Deleted" tab of the Fields section: who deleted a field, when, how long it can still be restored. */
export function DeletedFieldsList({
  panel,
  onOpenLive,
}: {
  panel: DeletedFieldsPanelProps;
  onOpenLive: (name: string) => void;
}) {
  const { copy, timeZone } = useSettings();
  const t = copy.fields.deleted;
  const [notes, setNotes] = useState<Record<string, RowNote>>({});
  const [dismissed, setDismissed] = useState<ReadonlySet<string>>(new Set());
  const [last, setLast] = useState<{ name: string; count: number } | null>(
    null,
  );

  async function restore(field: DeletedFieldRow) {
    setLast(null);
    setNotes(({ [field.id]: _gone, ...rest }) => rest);
    const result = await panel.onRestore(field.id);
    if (result === "restored")
      setLast({ name: field.name, count: field.valueCount });
    else setNotes((current) => ({ ...current, [field.id]: { kind: result } }));
  }

  if (panel.status === "loading")
    return (
      <div
        role="status"
        aria-label={t.loading}
        className="overflow-hidden rounded-xl border border-line bg-popover"
      >
        {[0, 1, 2].map((row) => (
          <div
            key={row}
            className="h-[72px] animate-pulse border-t border-line first:border-t-0"
          >
            <div className="m-4 h-4 w-1/3 rounded bg-state-hover-strong" />
          </div>
        ))}
      </div>
    );
  if (panel.status === "error")
    return (
      <div
        role="alert"
        className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-compact leading-[1.45] text-danger-text"
      >
        <CircleAlert
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0"
          strokeWidth={1.8}
        />
        <span className="flex min-w-0 flex-1 flex-col gap-px">
          <b className="font-semibold text-foreground">{t.loadError}</b>
          <span>{t.loadErrorBody}</span>
        </span>
        <button
          type="button"
          onClick={panel.onReload}
          className={BUTTON_CLASS.outline}
        >
          {t.reload}
        </button>
      </div>
    );

  const rows = panel.fields.filter((field) => !dismissed.has(field.id));
  return (
    <div className="flex flex-col gap-4">
      {last ? (
        <div
          role="status"
          className="flex items-start gap-2.5 rounded-[10px] bg-success-subtle px-3.5 py-2.5 text-compact leading-[1.45] text-success-subtle-foreground"
        >
          <CircleCheck
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0"
            strokeWidth={1.8}
          />
          <span className="flex min-w-0 flex-1 flex-col gap-px">
            <b className="font-semibold text-foreground">
              {fillCopy(t.done, { name: last.name })}
            </b>
            <span>
              {last.count > 0
                ? fillCopy(t.doneBody, { count: last.count })
                : t.doneBodyNone}
            </span>
          </span>
          <button
            type="button"
            onClick={() => onOpenLive(last.name)}
            className={BUTTON_CLASS.outline}
          >
            {t.openField}
          </button>
        </div>
      ) : null}
      {rows.length === 0 ? (
        <div className="flex flex-col items-center gap-1 rounded-xl border border-line bg-popover px-6 py-10 text-center">
          <Info
            aria-hidden="true"
            className="size-5 text-meta-foreground"
            strokeWidth={1.8}
          />
          <p className="m-0 text-body font-semibold">{t.emptyTitle}</p>
          <p className="m-0 max-w-[52ch] text-compact text-meta-foreground">
            {t.emptyBody}
          </p>
        </div>
      ) : (
        <div
          role="table"
          aria-label={t.label}
          className="overflow-hidden rounded-xl border border-line bg-popover text-[14px]"
        >
          <div
            role="row"
            className={cn(
              GRID,
              "h-9 border-b border-line bg-background text-[11.5px] font-semibold text-meta-foreground",
            )}
          >
            <span role="columnheader">{t.columns.name}</span>
            <span role="columnheader">{t.columns.type}</span>
            <span role="columnheader" className="max-[1100px]:hidden">
              {t.columns.by}
            </span>
            <span role="columnheader">{t.columns.left}</span>
            <span role="columnheader" />
          </div>
          {rows.map((field) => {
            const now = Date.now();
            const days = Math.floor(
              (Date.parse(field.restorableUntil) - now) / DAY,
            );
            const gone =
              Date.parse(field.restorableUntil) <= now ||
              notes[field.id]?.kind === "expired";
            const note = notes[field.id];
            const busy = panel.restoringId === field.id;
            return (
              <div
                key={field.id}
                role="row"
                className={cn(
                  GRID,
                  "min-h-[72px] border-t border-line py-2.5 first:border-t-0",
                  note && note.kind !== "done" && "bg-danger-subtle",
                  busy && "bg-state-selected",
                )}
              >
                <span
                  role="cell"
                  className="flex min-w-0 items-center gap-[11px]"
                >
                  <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-state-hover-strong text-muted-foreground">
                    <RowIcon type={field.type} />
                  </span>
                  <span className="min-w-0">
                    <b className="block truncate text-[14px] font-semibold">
                      {field.name}
                    </b>
                    <small className="block text-xs text-meta-foreground">
                      {field.valueCount === 0
                        ? t.metaNone
                        : field.valueCount === 1
                          ? t.metaOne
                          : fillCopy(t.meta, { count: field.valueCount })}
                    </small>
                  </span>
                </span>
                <span role="cell" className="text-compact">
                  {typeName(copy, field.type)}
                </span>
                <span role="cell" className="text-compact max-[1100px]:hidden">
                  <span className="flex items-center gap-2">
                    <span
                      aria-hidden="true"
                      className="grid size-[22px] shrink-0 place-items-center rounded-full bg-state-hover-strong text-[8px] font-bold text-muted-foreground"
                    >
                      {initialsOf(field.deletedByName ?? t.unknownPerson)}
                    </span>
                    <span>
                      <span className="font-medium">
                        {field.deletedByName ?? t.unknownPerson}
                      </span>
                      <small className="block text-xs text-meta-foreground">
                        {fillCopy(t.deletedOn, {
                          date: fullDay(field.deletedAt, timeZone),
                        })}
                      </small>
                    </span>
                  </span>
                </span>
                <span role="cell">
                  <span
                    className={cn(
                      "inline-flex h-[22px] items-center rounded-md px-2 text-xs font-semibold whitespace-nowrap",
                      gone
                        ? "bg-danger-subtle text-danger-text"
                        : days < 3
                          ? "bg-warning-subtle text-warning-text"
                          : "bg-state-hover-strong text-muted-foreground",
                    )}
                  >
                    {gone
                      ? t.gone
                      : days < 1
                        ? t.leftToday
                        : days === 1
                          ? t.leftOne
                          : fillCopy(t.left, { count: days })}
                  </span>
                </span>
                <span role="cell" className="justify-self-end">
                  <button
                    type="button"
                    aria-disabled={busy || gone || undefined}
                    aria-label={fillCopy(copy.fields.restoreLabel, {
                      name: field.name,
                    })}
                    onClick={
                      busy || gone ? undefined : () => void restore(field)
                    }
                    className={cn(
                      BUTTON_CLASS.outline,
                      "h-7 px-2.5 text-xs aria-disabled:cursor-default aria-disabled:opacity-60",
                    )}
                  >
                    {busy ? t.restoring : t.restore}
                  </button>
                </span>
                {note && note.kind !== "done" ? (
                  <div
                    role="alert"
                    className="col-span-full flex items-start gap-2 pl-[43px] text-[12.5px] leading-normal font-medium text-danger-text"
                  >
                    <CircleAlert
                      aria-hidden="true"
                      className="mt-0.5 size-3.5 shrink-0"
                      strokeWidth={1.8}
                    />
                    <span>
                      {note.kind === "name-taken"
                        ? fillCopy(t.nameTaken, { name: field.name })
                        : note.kind === "expired"
                          ? t.expired
                          : note.kind === "forbidden"
                            ? t.forbidden
                            : fillCopy(t.failed, { name: field.name })}
                    </span>
                    <span className="ml-auto flex shrink-0 gap-2">
                      {note.kind === "name-taken" ? (
                        <button
                          type="button"
                          onClick={() => onOpenLive(field.name)}
                          className={cn(
                            BUTTON_CLASS.outline,
                            "h-7 px-2.5 text-xs",
                          )}
                        >
                          {t.openLive}
                        </button>
                      ) : null}
                      {note.kind === "expired" ? (
                        <button
                          type="button"
                          onClick={() =>
                            setDismissed((current) =>
                              new Set(current).add(field.id),
                            )
                          }
                          className={cn(
                            BUTTON_CLASS.outline,
                            "h-7 px-2.5 text-xs",
                          )}
                        >
                          {t.dismiss}
                        </button>
                      ) : null}
                      {note.kind === "failed" ? (
                        <button
                          type="button"
                          onClick={() => void restore(field)}
                          className={cn(
                            BUTTON_CLASS.outline,
                            "h-7 px-2.5 text-xs",
                          )}
                        >
                          {t.retry}
                        </button>
                      ) : null}
                    </span>
                  </div>
                ) : null}
              </div>
            );
          })}
        </div>
      )}
      <p className="m-0 max-w-[90ch] text-[12.5px] leading-[1.55] text-meta-foreground">
        {t.note}
      </p>
    </div>
  );
}
