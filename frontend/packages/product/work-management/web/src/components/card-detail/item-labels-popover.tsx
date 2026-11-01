import { useId, useState, type ReactNode } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import type { CardLabel } from "@notrelix/work-management-core";
import { ITEM_LABEL_COLORS } from "../shared/item-display";

/** The label palette, in the order the swatches are drawn. */
const SWATCHES = [
  ["blue", ITEM_LABEL_COLORS.data],
  ["red", ITEM_LABEL_COLORS.red],
  ["green", ITEM_LABEL_COLORS.green],
  ["amber", ITEM_LABEL_COLORS.report],
  ["gray", ITEM_LABEL_COLORS.doc],
  ["violet", ITEM_LABEL_COLORS.test],
] as const;

type SwatchName = (typeof SWATCHES)[number][0];

/** What the picker says to offer creating a label; absent: the picker only toggles. */
export interface ItemLabelCreate {
  /** Creates the label and puts it on the item; resolves false when it did not go through. */
  readonly onCreate: (name: string, color: string) => Promise<boolean>;
  readonly searchLabel: string;
  readonly searchPlaceholder: string;
  /** `Create label “{{name}}”`. */
  readonly createText: string;
  readonly failedText: string;
  readonly retryText: string;
  /** "Tạo nhãn": the row that opens the name and colour form in place. */
  readonly newText: string;
  readonly nameLabel: string;
  readonly colorLabel: string;
  readonly colorNames: Readonly<Record<SwatchName, string>>;
  /** The form's submit ("Tạo và gắn") and cancel. */
  readonly submitText: string;
  readonly cancelText: string;
  /** "Quản lý nhãn" and where it goes (Board settings › Labels); absent: no link. */
  readonly manageText: string;
  readonly manageHref?: string;
  /** The colour a new label starts with. */
  readonly defaultColor: string;
}

/** The picker's create option from a host's copy; absent without a way to create. */
export function labelCreate(
  t: {
    readonly labelSearch: string;
    readonly labelSearchPlaceholder: string;
    readonly createLabel: string;
    readonly createLabelFailed: string;
    readonly createLabelRetry: string;
    readonly newLabel: string;
    readonly labelName: string;
    readonly labelColor: string;
    readonly labelColors: Readonly<Record<SwatchName, string>>;
    readonly createLabelAction: string;
    readonly cancel: string;
    readonly manageLabels: string;
  },
  onCreate: ((name: string, color?: string) => Promise<boolean>) | undefined,
  manage: { readonly href?: string; readonly nextColor: string },
): ItemLabelCreate | undefined {
  return onCreate
    ? {
        onCreate,
        searchLabel: t.labelSearch,
        searchPlaceholder: t.labelSearchPlaceholder,
        createText: t.createLabel,
        failedText: t.createLabelFailed,
        retryText: t.createLabelRetry,
        newText: t.newLabel,
        nameLabel: t.labelName,
        colorLabel: t.labelColor,
        colorNames: t.labelColors,
        submitText: t.createLabelAction,
        cancelText: t.cancel,
        manageText: t.manageLabels,
        manageHref: manage.href,
        defaultColor: manage.nextColor,
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

function Swatches({
  create,
  value,
  onChange,
}: {
  create: ItemLabelCreate;
  value: string;
  onChange: (color: string) => void;
}) {
  const labelId = useId();
  return (
    <div className="flex items-center gap-2">
      <span id={labelId} className="sr-only">
        {create.colorLabel}
      </span>
      <div
        role="radiogroup"
        aria-labelledby={labelId}
        className="flex flex-wrap gap-1.5"
      >
        {SWATCHES.map(([name, color]) => (
          <button
            key={name}
            type="button"
            role="radio"
            aria-checked={value === color}
            aria-label={create.colorNames[name]}
            onClick={() => onChange(color)}
            style={{ backgroundColor: color }}
            className={cn(
              "size-5.5 cursor-pointer rounded-md border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              value === color &&
                "ring-2 ring-primary ring-offset-2 ring-offset-popover",
            )}
          />
        ))}
      </div>
    </div>
  );
}

/**
 * The board's labels as a toggle list; a pick adds or removes that label. With
 * `create`, the list has a search box and a "Tạo nhãn" row that grows into a
 * name and colour form in place; typed text that no label is named gets a
 * `Create label “…”` row with the colour choice under it. "Quản lý nhãn"
 * leads to Board settings › Labels.
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
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [color, setColor] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const errorId = useId();
  if (readOnly) return <>{children}</>;

  const needle = fold(query);
  const shown = needle
    ? labels.filter((entry) => fold(entry.name).includes(needle))
    : labels;
  const typed = (creating ? newName : query).trim();
  const picked = color ?? create?.defaultColor ?? ITEM_LABEL_COLORS.data;
  const canCreate =
    !!create &&
    typed !== "" &&
    !labels.some((entry) => fold(entry.name) === fold(typed));

  function reset() {
    setQuery("");
    setNewName("");
    setCreating(false);
    setColor(null);
    setFailed(false);
  }

  async function createLabel() {
    if (!create || !canCreate || busy) return;
    setBusy(true);
    setFailed(false);
    const ok = await create.onCreate(typed, picked).catch(() => false);
    setBusy(false);
    if (ok) reset();
    else setFailed(true);
  }

  const failure =
    failed && create ? (
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
    ) : null;

  return (
    <Popover
      onOpenChange={(open) => {
        if (!open) reset();
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-1">
        {create && !creating ? (
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
            {failure}
          </div>
        ) : null}
        {creating && create ? (
          <form
            className="m-1 flex flex-col gap-2.5 rounded-lg bg-state-selected p-2.5"
            onSubmit={(event) => {
              event.preventDefault();
              void createLabel();
            }}
          >
            <label className="flex flex-col gap-1 text-xs font-semibold text-meta-foreground">
              {create.nameLabel}
              <input
                type="text"
                autoFocus
                value={newName}
                aria-invalid={failed ? true : undefined}
                onChange={(event) => {
                  setNewName(event.target.value);
                  setFailed(false);
                }}
                className="box-border h-8 w-full rounded-md border border-line-strong bg-popover px-2 text-sm font-normal text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger"
              />
            </label>
            <Swatches create={create} value={picked} onChange={setColor} />
            {failure}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={reset}
                className="h-7 cursor-pointer rounded-md border-0 bg-transparent px-2.5 text-sm text-muted-foreground hover:bg-state-hover"
              >
                {create.cancelText}
              </button>
              <button
                type="submit"
                disabled={!canCreate || busy}
                className="h-7 cursor-pointer rounded-md border-0 bg-primary px-2.5 text-sm font-semibold text-primary-foreground disabled:cursor-default disabled:opacity-50"
              >
                {create.submitText}
              </button>
            </div>
          </form>
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
              <li className="flex flex-col gap-2 rounded-md bg-state-hover p-2">
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => void createLabel()}
                  className="flex w-full cursor-pointer items-center gap-2 border-0 bg-transparent p-0 text-left text-sm font-medium text-brand-subtle-foreground disabled:cursor-wait disabled:opacity-60"
                >
                  <Plus aria-hidden="true" className="size-3.5 shrink-0" />
                  <span className="min-w-0 flex-1 truncate">
                    {create.createText.replace("{{name}}", typed)}
                  </span>
                </button>
                <Swatches create={create} value={picked} onChange={setColor} />
              </li>
            ) : null}
          </ul>
        )}
        {labels.length === 0 && !creating ? (
          <p className="m-0 px-2 py-1.5 text-sm text-muted-foreground">
            {emptyText}
          </p>
        ) : null}
        {create && !creating ? (
          <button
            type="button"
            onClick={() => setCreating(true)}
            className="flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 py-1.5 text-left text-sm font-medium text-brand-subtle-foreground hover:bg-state-hover"
          >
            <Plus aria-hidden="true" className="size-3.5 shrink-0" />
            {create.newText}
          </button>
        ) : null}
        {create?.manageHref ? (
          <a
            href={create.manageHref}
            className="mt-0.5 block border-t border-line px-2 pt-2 pb-1 text-xs font-medium text-brand-subtle-foreground"
          >
            {create.manageText}
          </a>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
