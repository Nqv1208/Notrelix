import { useId, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  ITEM_LABEL_COLORS,
  ITEM_LABEL_TONE_CLASS,
  itemLabelTone,
  type ItemLabelTone,
} from "../shared/item-display";
import { useSettings } from "./board-settings-context";
import { fillCopy, plural } from "./board-settings-format";
import {
  BUTTON_CLASS,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  SectionHeader,
} from "./board-settings-parts";
import type {
  BoardLabelRow,
  BoardLabelsPanelProps,
} from "./board-settings-types";
import { WriteError, useWriteErrorProps } from "./board-settings-write-error";

/** The six tones the page offers, in the drawn order: blue, teal, violet, amber, rose, grey. */
const TONES: readonly ItemLabelTone[] = [
  "data",
  "training",
  "test",
  "report",
  "security",
  "doc",
];

const taken = (labels: readonly BoardLabelRow[], name: string, skipId = "") =>
  labels.some(
    (label) =>
      label.id !== skipId && label.name.toLowerCase() === name.toLowerCase(),
  );

/** Six colour choices (26 px squares), one per label tone; the current one is ringed. */
function ColorChoices({
  current,
  onPick,
  label,
  radio,
}: {
  current: string;
  onPick: (color: string) => void;
  label: string;
  radio?: boolean;
}) {
  const { copy } = useSettings();
  const currentTone = itemLabelTone({ name: "", color: current });
  return (
    <div
      role={radio ? "radiogroup" : "group"}
      aria-label={label}
      className="flex gap-1.5"
    >
      {TONES.map((tone) => (
        <button
          key={tone}
          type="button"
          role={radio ? "radio" : undefined}
          aria-checked={radio ? currentTone === tone : undefined}
          aria-pressed={radio ? undefined : currentTone === tone}
          aria-label={copy.labels.colors[tone]}
          onClick={() => onPick(ITEM_LABEL_COLORS[tone])}
          className={cn(
            "size-[26px] max-md:size-11 cursor-pointer rounded-[6px] border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-checked:outline-2 aria-checked:outline-solid aria-checked:outline-offset-2 aria-checked:outline-primary aria-pressed:outline-solid aria-pressed:outline-2 aria-pressed:outline-offset-2 aria-pressed:outline-primary",
            ITEM_LABEL_TONE_CLASS[tone],
          )}
        />
      ))}
    </div>
  );
}

const CHIP =
  "inline-flex h-5.5 max-w-full items-center truncate rounded-[6px] px-2 text-[12px] font-semibold";
const ROW_GRID =
  "grid grid-cols-[minmax(0,1fr)_140px_120px] items-center gap-2.5 px-3 max-md:grid-cols-[minmax(0,1fr)_auto_auto]";
const SMALL = "h-7 px-2.5 text-[12.5px]";

/** The "Nhãn" section: the labels of the board, with create, rename, recolour and delete. */
export function LabelsSection({ panel }: { panel: BoardLabelsPanelProps }) {
  const { copy, run } = useSettings();
  const t = copy.labels;
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState("");
  const [color, setColor] = useState(ITEM_LABEL_COLORS.data);
  const addErrorId = useId();
  const addFailed = useWriteErrorProps("label-add", addErrorId);
  const trimmed = name.trim();
  const duplicate = trimmed !== "" && taken(panel.labels, trimmed);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  async function add() {
    if (trimmed === "" || duplicate) return;
    const result = await run(() => panel.onCreate(trimmed, color), "label-add");
    if (result.ok) {
      setAdding(false);
      setName("");
    }
  }

  const close = () => {
    setAdding(false);
    setName("");
  };

  return (
    <section
      aria-labelledby="bs-labels-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-labels-h"
          title={t.title}
          intro={
            plural(t.summary_one, t.summary_other, panel.labels.length) +
            t.shared
          }
          actions={
            <button
              type="button"
              aria-expanded={adding}
              onClick={() => setAdding(true)}
              className={BUTTON_CLASS.primary}
            >
              <Plus aria-hidden="true" />
              {t.add}
            </button>
          }
        />

        {adding ? (
          <form
            aria-label={t.newLabel}
            className="mb-3.5 flex flex-col gap-3 rounded-[12px] border border-primary bg-popover p-3.5 shadow-[0_0_0_3px_color-mix(in_srgb,var(--primary)_14%,transparent)]"
            onSubmit={(event) => {
              event.preventDefault();
              void add();
            }}
          >
            <p className="m-0 text-metadata leading-[1.45] font-semibold text-meta-foreground">
              {t.newLabel}
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <span className="w-16 text-crumb font-semibold text-muted-foreground">
                {t.nameShort}
              </span>
              <input
                autoFocus
                value={name}
                aria-label={t.nameLabel}
                placeholder={t.namePlaceholder}
                {...(duplicate
                  ? {
                      "aria-invalid": true,
                      "aria-describedby": `${addErrorId}-dup`,
                    }
                  : addFailed)}
                onChange={(event) => setName(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Escape") {
                    event.stopPropagation();
                    close();
                  }
                }}
                className={cn(INPUT_CLASS, "w-[260px] max-md:w-full")}
              />
              <span className="text-crumb font-semibold text-muted-foreground">
                {t.colorLabel}
              </span>
              <ColorChoices
                radio
                label={t.colorLabel}
                current={color}
                onPick={setColor}
              />
              <span
                className={cn(
                  CHIP,
                  ITEM_LABEL_TONE_CLASS[itemLabelTone({ name: "", color })],
                )}
              >
                {trimmed || t.namePlaceholder}
              </span>
              <span className="ml-auto flex gap-2">
                <button
                  type="button"
                  onClick={close}
                  className={BUTTON_CLASS.ghost}
                >
                  {copy.common.cancel}
                </button>
                <button
                  type="submit"
                  disabled={trimmed === "" || duplicate}
                  className={BUTTON_CLASS.primary}
                >
                  {t.addConfirm}
                </button>
              </span>
            </div>
            <div className="empty:hidden">
              {duplicate ? (
                <p
                  id={`${addErrorId}-dup`}
                  className="m-0 text-crumb text-danger-text"
                >
                  {t.nameTaken}
                </p>
              ) : (
                <WriteError scope="label-add" id={addErrorId} />
              )}
            </div>
          </form>
        ) : null}

        {panel.status === "loading" ? (
          <div
            role="status"
            aria-label={t.loading}
            className="flex flex-col gap-2"
          >
            <Skeleton className="h-11 rounded-[10px]" />
            <Skeleton className="h-11 rounded-[10px]" />
          </div>
        ) : panel.status === "error" ? (
          <div
            role="alert"
            className="flex items-center gap-3 rounded-[10px] border border-line bg-popover px-3.5 py-3 text-item-ui"
          >
            <span>{t.error}</span>
            <button
              type="button"
              onClick={panel.onReload}
              className={BUTTON_CLASS.outline}
            >
              {t.retry}
            </button>
          </div>
        ) : panel.labels.length === 0 ? (
          <div className="rounded-[10px] border border-dashed border-line-strong bg-popover px-4 py-6 text-center">
            <b className="block text-item-ui font-semibold">{t.empty}</b>
            <span className={HINT_CLASS}>{t.emptyHint}</span>
          </div>
        ) : (
          <div className="overflow-hidden rounded-[12px] border border-line bg-popover">
            <div
              aria-hidden="true"
              className={cn(
                ROW_GRID,
                "min-h-[34px] bg-background text-metadata leading-[1.45] font-semibold text-meta-foreground",
              )}
            >
              <span>{t.columnLabel}</span>
              <span>{t.columnCount}</span>
              <span />
            </div>
            <ul aria-label={t.list} className="m-0 list-none p-0">
              {panel.labels.map((label) => (
                <LabelRow
                  key={label.id}
                  label={label}
                  all={panel.labels}
                  deleting={deleteId === label.id}
                  onDelete={() => setDeleteId(label.id)}
                  onDeleteDone={() => setDeleteId(null)}
                  panel={panel}
                />
              ))}
            </ul>
          </div>
        )}
      </div>
    </section>
  );
}

function LabelRow({
  label,
  all,
  deleting,
  onDelete,
  onDeleteDone,
  panel,
}: {
  label: BoardLabelRow;
  all: readonly BoardLabelRow[];
  deleting: boolean;
  onDelete: () => void;
  onDeleteDone: () => void;
  panel: BoardLabelsPanelProps;
}) {
  const { copy, run } = useSettings();
  const t = copy.labels;
  const [renaming, setRenaming] = useState(false);
  const [name, setName] = useState(label.name);
  const [color, setColor] = useState(label.color);
  const [palette, setPalette] = useState(false);
  const scope = `label:${label.id}`;
  const errorId = useId();
  const failed = useWriteErrorProps(scope, errorId);
  const trimmed = name.trim();
  const duplicate = taken(all, trimmed, label.id);
  const tone = itemLabelTone(label);

  const stop = () => {
    setName(label.name);
    setColor(label.color);
    setRenaming(false);
  };

  async function commit() {
    const changes = {
      ...(trimmed !== label.name ? { name: trimmed } : {}),
      ...(color !== label.color ? { color } : {}),
    };
    if (trimmed === "" || Object.keys(changes).length === 0) return stop();
    if (duplicate) return;
    // A failed save stays open with what was typed; the error offers Retry.
    const result = await run(() => panel.onUpdate(label.id, changes), scope);
    if (result.ok) setRenaming(false);
  }

  async function confirmDelete() {
    const result = await run(() => panel.onDelete(label.id), scope);
    if (result.ok) onDeleteDone();
  }

  const count = label.itemCount;
  const [before, after] = t.deleteQuestion.split("{{name}}");
  return (
    <li
      className={cn(
        "border-t border-line",
        deleting && "bg-danger-subtle",
        renaming && "bg-row-selected shadow-[inset_0_0_0_1.5px_var(--primary)]",
      )}
    >
      <div
        className={cn(
          ROW_GRID,
          "text-item-ui",
          renaming ? "min-h-[52px]" : "min-h-[46px]",
          deleting && "min-h-0 py-2.5",
        )}
      >
        {renaming ? (
          <span className="flex items-center gap-2">
            <input
              autoFocus
              value={name}
              aria-label={t.nameLabel}
              {...(duplicate
                ? { "aria-invalid": true, "aria-describedby": `${errorId}-dup` }
                : failed)}
              onChange={(event) => setName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void commit();
                }
                if (event.key === "Escape") {
                  event.stopPropagation();
                  stop();
                }
              }}
              className={cn(INPUT_CLASS, "w-[220px] max-md:w-full")}
            />
            <Popover open={palette} onOpenChange={setPalette}>
              <PopoverTrigger asChild>
                <button
                  type="button"
                  aria-label={fillCopy(t.recolor, {
                    name: copy.labels.colors[
                      itemLabelTone({ name: "", color })
                    ],
                  })}
                  className={cn(
                    "size-[26px] max-md:size-11 shrink-0 cursor-pointer rounded-[6px] border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    ITEM_LABEL_TONE_CLASS[itemLabelTone({ name: "", color })],
                  )}
                />
              </PopoverTrigger>
              <PopoverContent align="start" className="w-auto p-2">
                <ColorChoices
                  label={t.colorLabel}
                  current={color}
                  onPick={(next) => {
                    setColor(next);
                    setPalette(false);
                  }}
                />
              </PopoverContent>
            </Popover>
          </span>
        ) : (
          <span className="min-w-0">
            <span className={cn(CHIP, ITEM_LABEL_TONE_CLASS[tone])}>
              {label.name || label.color}
            </span>
          </span>
        )}
        <span className="text-crumb text-meta-foreground">
          {count === undefined
            ? null
            : plural(t.onItems_one, t.onItems_other, count)}
        </span>
        {renaming ? (
          <span className="flex justify-end gap-1.5">
            <button
              type="button"
              onClick={stop}
              className={cn(BUTTON_CLASS.ghost, SMALL)}
            >
              {copy.common.cancel}
            </button>
            <button
              type="button"
              disabled={trimmed === "" || duplicate}
              onClick={() => void commit()}
              className={cn(BUTTON_CLASS.primary, SMALL)}
            >
              {t.save}
            </button>
          </span>
        ) : deleting ? (
          <span />
        ) : (
          <span className="flex justify-end">
            <button
              type="button"
              aria-label={fillCopy(t.rename, { name: label.name })}
              onClick={() => {
                setName(label.name);
                setColor(label.color);
                setRenaming(true);
              }}
              className={cn(ICON_BUTTON_CLASS, "size-[30px]")}
            >
              <Pencil aria-hidden="true" />
            </button>
            <button
              type="button"
              aria-label={fillCopy(t.delete, { name: label.name })}
              aria-expanded={deleting}
              onClick={onDelete}
              className={cn(ICON_BUTTON_CLASS, "size-[30px]")}
            >
              <Trash2 aria-hidden="true" />
            </button>
          </span>
        )}
      </div>
      <div className="px-3 empty:hidden">
        {duplicate ? (
          <p
            id={`${errorId}-dup`}
            className="m-0 mb-1 text-crumb text-danger-text"
          >
            {t.nameTaken}
          </p>
        ) : (
          <WriteError scope={scope} id={errorId} />
        )}
      </div>
      {deleting ? (
        <div className="px-3 pb-2.5">
          <div
            role="alertdialog"
            aria-label={fillCopy(t.deleteQuestion, { name: label.name })}
            className="flex items-center gap-3 rounded-[10px] border border-danger/30 bg-popover p-3"
          >
            <p className="m-0 flex-1 text-item-small">
              {before}
              <b className="font-semibold">{label.name}</b>
              {after}{" "}
              {count === undefined
                ? t.deleteBodyUnknown
                : count === 0
                  ? t.deleteBodyNone
                  : plural(t.deleteBody_one, t.deleteBody_other, count)}
            </p>
            <button
              type="button"
              onClick={onDeleteDone}
              className={cn(BUTTON_CLASS.outline, SMALL)}
            >
              {t.keep}
            </button>
            <button
              type="button"
              onClick={() => void confirmDelete()}
              className={cn(BUTTON_CLASS.danger, SMALL)}
            >
              {t.deleteConfirm}
            </button>
          </div>
        </div>
      ) : null}
    </li>
  );
}
