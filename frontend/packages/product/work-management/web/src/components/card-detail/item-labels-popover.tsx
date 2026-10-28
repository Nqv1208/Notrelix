import { useId, useState, type ReactNode } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import type { CardLabel } from "@notrelix/work-management-core";

/** What the picker says to offer creating a label; absent: the picker only toggles. */
export interface ItemLabelCreate {
  /** Creates the label and puts it on the item; resolves false when it did not go through. */
  readonly onCreate: (name: string) => Promise<boolean>;
  readonly searchLabel: string;
  readonly searchPlaceholder: string;
  /** `Create label “{{name}}”`. */
  readonly createText: string;
  readonly failedText: string;
  readonly retryText: string;
}

/** The picker's create option from a host's copy; absent without a way to create. */
export function labelCreate(
  t: {
    readonly labelSearch: string;
    readonly labelSearchPlaceholder: string;
    readonly createLabel: string;
    readonly createLabelFailed: string;
    readonly createLabelRetry: string;
  },
  onCreate: ((name: string) => Promise<boolean>) | undefined,
): ItemLabelCreate | undefined {
  return onCreate
    ? {
        onCreate,
        searchLabel: t.labelSearch,
        searchPlaceholder: t.labelSearchPlaceholder,
        createText: t.createLabel,
        failedText: t.createLabelFailed,
        retryText: t.createLabelRetry,
      }
    : undefined;
}

const fold = (text: string) =>
  text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .trim();

/**
 * The board's labels as a toggle list; a pick adds or removes that label. With
 * `create`, the list has a search box, and typed text that no label is named
 * gets a `Create label “…”` row that creates it and puts it on the item.
 */
export function ItemLabelsPopover({
  labels,
  selectedIds,
  label,
  emptyText,
  readOnly,
  onToggle,
  create,
  children,
}: {
  labels: readonly CardLabel[];
  selectedIds: ReadonlySet<string>;
  label: string;
  /** Said when the board has no labels. */
  emptyText: string;
  readOnly: boolean;
  onToggle: (labelId: string, on: boolean) => void;
  create?: ItemLabelCreate;
  children: ReactNode;
}) {
  const [query, setQuery] = useState("");
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const errorId = useId();
  if (readOnly) return <>{children}</>;

  const needle = fold(query);
  const shown = needle
    ? labels.filter((entry) => fold(entry.name).includes(needle))
    : labels;
  const typed = query.trim();
  const canCreate =
    !!create &&
    typed !== "" &&
    !labels.some((entry) => fold(entry.name) === needle);

  async function createLabel() {
    if (!create || !canCreate || busy) return;
    setBusy(true);
    setFailed(false);
    const ok = await create.onCreate(typed).catch(() => false);
    setBusy(false);
    if (ok) setQuery("");
    else setFailed(true);
  }

  return (
    <Popover
      onOpenChange={(open) => {
        if (!open) {
          setQuery("");
          setFailed(false);
        }
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-1">
        {create ? (
          <div className="p-1 pb-1.5">
            <input
              type="text"
              autoFocus
              value={query}
              placeholder={create.searchPlaceholder}
              aria-label={create.searchLabel}
              aria-describedby={failed ? errorId : undefined}
              aria-invalid={failed ? true : undefined}
              onChange={(event) => {
                setQuery(event.target.value);
                setFailed(false);
              }}
              onKeyDown={(event) => {
                if (event.key !== "Enter") return;
                event.preventDefault();
                if (shown.length === 0) void createLabel();
                else {
                  const first = shown[0];
                  if (first) onToggle(first.id, !selectedIds.has(first.id));
                }
              }}
              className="box-border h-8 w-full rounded-md border border-line-strong bg-popover px-2 text-sm outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
            />
            {failed ? (
              <p
                id={errorId}
                role="alert"
                className="m-0 mt-1 flex flex-wrap items-center gap-x-2 text-xs text-danger-text"
              >
                {create.failedText}
                <button
                  type="button"
                  onClick={() => void createLabel()}
                  className="cursor-pointer border-0 bg-transparent p-0 text-xs font-medium text-brand-subtle-foreground underline"
                >
                  {create.retryText}
                </button>
              </p>
            ) : null}
          </div>
        ) : null}
        {labels.length === 0 && !create ? (
          <p className="m-0 px-2 py-1.5 text-sm text-muted-foreground">
            {emptyText}
          </p>
        ) : (
          <ul aria-label={label} className="flex flex-col gap-0.5">
            {shown.map((entry) => {
              const selected = selectedIds.has(entry.id);
              return (
                <li key={entry.id}>
                  <button
                    type="button"
                    aria-pressed={selected}
                    className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-state-hover aria-pressed:bg-state-selected"
                    onClick={() => onToggle(entry.id, !selected)}
                  >
                    <span
                      aria-hidden="true"
                      className="size-3 shrink-0 rounded-full"
                      style={{ backgroundColor: entry.color }}
                    />
                    <span className="min-w-0 flex-1 truncate">
                      {entry.name || entry.color}
                    </span>
                  </button>
                </li>
              );
            })}
            {canCreate && create ? (
              <li>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => void createLabel()}
                  className="flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 py-1.5 text-left text-sm font-medium text-brand-subtle-foreground hover:bg-state-hover disabled:cursor-wait disabled:opacity-60"
                >
                  <Plus aria-hidden="true" className="size-3.5 shrink-0" />
                  <span className="min-w-0 flex-1 truncate">
                    {create.createText.replace("{{name}}", typed)}
                  </span>
                </button>
              </li>
            ) : null}
          </ul>
        )}
        {create && labels.length === 0 && !canCreate ? (
          <p className="m-0 px-2 py-1.5 text-sm text-muted-foreground">
            {emptyText}
          </p>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
