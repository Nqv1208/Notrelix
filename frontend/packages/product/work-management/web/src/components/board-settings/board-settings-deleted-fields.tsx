import { useEffect, useState } from "react";
import { Check, CircleAlert, Info, Undo2 } from "lucide-react";
import { cn, useMediaQuery } from "@notrelix/ui-web";
import { useSettings } from "./board-settings-context";
import { fillCopy, typeName } from "./board-settings-format";
import { BUTTON_CLASS, fieldIcon } from "./board-settings-parts";
import type {
  DeletedFieldRow,
  DeletedFieldsPanelProps,
} from "./board-settings-types";

const GRID =
  "grid grid-cols-[minmax(0,1.6fr)_150px_210px_130px_150px] items-center gap-3.5 px-4 max-[1100px]:grid-cols-[minmax(0,1fr)_120px_130px]";
const DAY = 86_400_000;
/** The 50 px full-width button of a phone card. */
const CARD_BUTTON =
  "h-[50px] w-full justify-center gap-1.5 rounded-[12px] border-line-strong px-4 text-[15px] font-medium max-md:h-[50px] [&_svg]:size-5";

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

function Icon({ type }: { type: DeletedFieldRow["type"] }) {
  const Glyph = fieldIcon(type, "");
  return <Glyph aria-hidden="true" className="size-5" strokeWidth={1.8} />;
}

/** Days left of a field's 30 days, and whether the window is closed (an expired note closes it early). */
function windowOf(field: DeletedFieldRow, note: RowNote | undefined) {
  const now = Date.now();
  const days = Math.floor((Date.parse(field.restorableUntil) - now) / DAY);
  return {
    days,
    gone: Date.parse(field.restorableUntil) <= now || note?.kind === "expired",
  };
}

/**
 * The deleted fields at phone width, one card each: what the field is, how long it can still be restored, who
 * deleted it and when, the refusal inside the card, and a 44 px Restore under it. Same facts as the table rows.
 */
function DeletedFieldCards({
  rows,
  notes,
  restoringId,
  onRestore,
  onOpenLive,
  onDismiss,
}: {
  rows: readonly DeletedFieldRow[];
  notes: Record<string, RowNote>;
  restoringId?: string;
  onRestore: (field: DeletedFieldRow) => void;
  onOpenLive: (name: string) => void;
  onDismiss: (id: string) => void;
}) {
  const { copy, timeZone } = useSettings();
  const t = copy.fields.deleted;
  return (
    <div role="list" aria-label={t.label} className="flex flex-col gap-3.5">
      {rows.map((field) => {
        const note = notes[field.id];
        const { days, gone } = windowOf(field, note);
        const busy = restoringId === field.id;
        const shortDate = fullDay(field.deletedAt, timeZone).slice(0, 5);
        const failed = note && note.kind !== "done" ? note : null;
        return (
          <div
            key={field.id}
            role="listitem"
            className="flex flex-col gap-2.5 rounded-[14px] border border-line bg-popover px-4 py-3.5"
          >
            <div className="flex items-center gap-2.5">
              <span className="grid size-9 shrink-0 place-items-center rounded-[10px] bg-state-hover-strong text-muted-foreground">
                <Icon type={field.type} />
              </span>
              <span className="min-w-0 flex-1 text-[15px] leading-[1.45]">
                <b className="font-[650]">{field.name}</b>
                <br />
                <small className="text-[13px] text-meta-foreground">
                  <span className="font-mono">{field.key}</span>
                  {" · "}
                  {typeName(copy, field.type)}
                </small>
              </span>
              <span
                className={cn(
                  "inline-flex h-6 items-center rounded-[7px] px-[9px] text-[12.5px] font-semibold whitespace-nowrap",
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
            </div>
            <small className="text-[13px] leading-normal text-meta-foreground">
              {fillCopy(t.cardMeta, {
                values:
                  field.valueCount === 0
                    ? t.metaNone
                    : field.valueCount === 1
                      ? t.metaOne
                      : fillCopy(t.meta, { count: field.valueCount }),
                date: shortDate,
                by: field.deletedByName ?? t.unknownPerson,
              })}
            </small>
            {failed ? (
              <div
                role="alert"
                className="flex items-start gap-2 text-[13.5px] leading-normal font-medium text-danger-text"
              >
                <CircleAlert
                  aria-hidden="true"
                  className="mt-[3px] size-[13px] shrink-0"
                  strokeWidth={1.8}
                />
                <span>
                  {failed.kind === "name-taken"
                    ? fillCopy(t.nameTaken, { name: field.name })
                    : failed.kind === "expired"
                      ? t.expired
                      : failed.kind === "forbidden"
                        ? t.forbidden
                        : fillCopy(t.failed, { name: field.name })}
                </span>
              </div>
            ) : null}
            {failed?.kind === "name-taken" ? (
              <button
                type="button"
                onClick={() => onOpenLive(field.name)}
                className={cn(BUTTON_CLASS.outline, CARD_BUTTON)}
              >
                {t.openLive}
              </button>
            ) : null}
            {failed?.kind === "expired" ? (
              <button
                type="button"
                onClick={() => onDismiss(field.id)}
                className={cn(BUTTON_CLASS.outline, CARD_BUTTON)}
              >
                {t.dismiss}
              </button>
            ) : null}
            <button
              type="button"
              aria-disabled={busy || gone || undefined}
              aria-label={fillCopy(copy.fields.restoreLabel, {
                name: field.name,
              })}
              onClick={busy || gone ? undefined : () => onRestore(field)}
              className={cn(
                BUTTON_CLASS.outline,
                CARD_BUTTON,
                "aria-disabled:cursor-default aria-disabled:opacity-60",
              )}
            >
              {busy ? null : <Undo2 aria-hidden="true" className="size-5" />}
              {busy ? t.restoring : t.restore}
            </button>
          </div>
        );
      })}
    </div>
  );
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
  const phone = useMediaQuery("(max-width: 767px)");
  const t = copy.fields.deleted;
  const [notes, setNotes] = useState<Record<string, RowNote>>({});
  const [dismissed, setDismissed] = useState<ReadonlySet<string>>(new Set());
  const [last, setLast] = useState<{ name: string; count: number } | null>(
    null,
  );

  // The toast leaves by itself; "Mở trường" is the one way on from it.
  useEffect(() => {
    if (!last) return;
    const timer = window.setTimeout(() => setLast(null), 8000);
    return () => window.clearTimeout(timer);
  }, [last]);

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
          className="fixed inset-x-4 bottom-6 z-toast flex items-center gap-3 rounded-[12px] bg-inverse-surface px-3.5 py-3 text-[14px] leading-[1.4] text-inverse-foreground shadow-[0_12px_32px_rgba(0,0,0,0.28)] md:inset-x-auto md:bottom-7 md:left-1/2 md:w-max md:max-w-[760px] md:-translate-x-1/2 md:rounded-[10px] md:py-[11px] md:text-[13.5px] md:leading-[1.45] md:whitespace-nowrap"
        >
          <Check
            aria-hidden="true"
            className="size-5 shrink-0 md:size-4"
            strokeWidth={1.8}
          />
          <span className="min-w-0 flex-1">
            <b>{fillCopy(t.done, { name: last.name })}</b>
            <br />
            <span className="text-[13px] opacity-85">
              {last.count > 0
                ? fillCopy(phone ? t.doneBodyShort : t.doneBody, {
                    count: last.count,
                  })
                : t.doneBodyNone}
            </span>
          </span>
          <button
            type="button"
            onClick={() => onOpenLive(last.name)}
            className="inline-flex h-11 cursor-pointer items-center border-0 bg-transparent px-1.5 font-semibold text-[#9CC4F7] focus-visible:outline-2 focus-visible:outline-ring md:h-auto"
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
      ) : phone ? (
        <DeletedFieldCards
          rows={rows}
          notes={notes}
          restoringId={panel.restoringId}
          onRestore={(field) => void restore(field)}
          onOpenLive={onOpenLive}
          onDismiss={(id) =>
            setDismissed((current) => new Set(current).add(id))
          }
        />
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
              "h-[37px] border-b border-line bg-background text-[11.5px] font-semibold text-meta-foreground",
            )}
          >
            <span role="columnheader">{t.columns.name}</span>
            <span role="columnheader" className="max-[1100px]:hidden">
              {t.columns.type}
            </span>
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
                  "border-t border-line py-2.5 [&:nth-child(2)]:border-t-0",
                  !note && "min-h-[93px] [&:nth-child(2)]:min-h-[92px]",
                  note && note.kind !== "done" && "bg-danger-subtle",
                  busy && "bg-state-selected",
                )}
              >
                <span
                  role="cell"
                  className="flex min-w-0 items-center gap-[11px]"
                >
                  <span className="grid size-8 shrink-0 place-items-center rounded-[8px] bg-state-hover-strong text-muted-foreground">
                    <RowIcon type={field.type} />
                  </span>
                  <span className="min-w-0">
                    <b className="block truncate text-[14px] leading-[1.45] font-semibold">
                      {field.name}
                    </b>
                    <small className="block text-xs leading-[1.45] text-meta-foreground">
                      <span className="font-mono">{field.key}</span>
                      <span className="hidden max-[1100px]:inline">
                        {" · "}
                        {typeName(copy, field.type)}
                      </span>
                      {" · "}
                      {field.valueCount === 0
                        ? t.metaNone
                        : field.valueCount === 1
                          ? t.metaOne
                          : fillCopy(t.meta, { count: field.valueCount })}
                    </small>
                  </span>
                </span>
                <span role="cell" className="text-compact max-[1100px]:hidden">
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
                      "inline-flex h-[22px] items-center rounded-[6px] px-2 text-xs font-semibold whitespace-nowrap",
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
                      "h-7 px-2.5 text-[12.5px] aria-disabled:cursor-default aria-disabled:opacity-60",
                    )}
                  >
                    {busy ? t.restoring : t.restore}
                  </button>
                </span>
                {note && note.kind !== "done" ? (
                  <div role="cell" className="col-span-full">
                    <div
                      role="alert"
                      className="flex items-start gap-1.5 pl-[43px] text-[12.5px] leading-normal font-medium text-danger-text"
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
                              "h-7 px-2.5 text-[12.5px]",
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
                              "h-7 px-2.5 text-[12.5px]",
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
                              "h-7 px-2.5 text-[12.5px]",
                            )}
                          >
                            {t.retry}
                          </button>
                        ) : null}
                      </span>
                    </div>
                  </div>
                ) : null}
              </div>
            );
          })}
        </div>
      )}
      <p className="m-0 max-w-[90ch] text-[12.5px] leading-[1.55] text-meta-foreground max-md:mt-[11px] max-md:text-[13px]">
        {phone ? t.noteShort : t.note}
      </p>
    </div>
  );
}
