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
  SectionLine,
} from "./board-settings-parts";
import type {
  BoardLabelRow,
  BoardLabelsPanelProps,
} from "./board-settings-types";
import { WriteError, useWriteErrorProps } from "./board-settings-write-error";

const TONES = Object.keys(ITEM_LABEL_COLORS) as ItemLabelTone[];

const taken = (labels: readonly BoardLabelRow[], name: string, skipId = "") =>
  labels.some(
    (label) =>
      label.id !== skipId && label.name.toLowerCase() === name.toLowerCase(),
  );

/** Six colour choices, one per label tone; the current one is marked. */
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
            "size-5.5 cursor-pointer rounded-[6px] border-0 p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-checked:shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--foreground)] aria-pressed:shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--foreground)]",
            ITEM_LABEL_TONE_CLASS[tone],
          )}
        />
      ))}
    </div>
  );
}

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
          intro={t.intro}
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
            className="mb-3 flex flex-wrap items-center gap-2"
            onSubmit={(event) => {
              event.preventDefault();
              void add();
            }}
          >
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
              className={cn(INPUT_CLASS, "min-w-0 flex-1")}
            />
            <ColorChoices
              radio
              label={t.colorLabel}
              current={color}
              onPick={setColor}
            />
            <button
              type="button"
              onClick={close}
              className={BUTTON_CLASS.outline}
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
            <div className="basis-full empty:hidden">
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

        <SectionLine
          title={t.list}
          count={plural(t.summary_one, t.summary_other, panel.labels.length)}
          note=""
        />

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
          <ul
            aria-label={t.list}
            className="m-0 flex list-none flex-col overflow-hidden rounded-[10px] border border-line bg-popover p-0"
          >
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
        )}
        <p className={cn(HINT_CLASS, "mt-2.5 mb-0 px-0.5")}>{t.footnote}</p>
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
  const scope = `label:${label.id}`;
  const errorId = useId();
  const failed = useWriteErrorProps(scope, errorId);
  const trimmed = name.trim();
  const duplicate = taken(all, trimmed, label.id);
  const tone = itemLabelTone(label);

  async function commit() {
    if (trimmed === "" || trimmed === label.name) {
      setName(label.name);
      setRenaming(false);
      return;
    }
    if (duplicate) return;
    // A failed rename stays open with the typed name; the error offers Retry.
    const result = await run(
      () => panel.onUpdate(label.id, { name: trimmed }),
      scope,
    );
    if (result.ok) setRenaming(false);
  }

  async function confirmDelete() {
    const result = await run(() => panel.onDelete(label.id), scope);
    if (result.ok) onDeleteDone();
  }

  const count = label.itemCount;
  return (
    <li className="border-t border-line first:border-t-0">
      <div className="flex min-h-[47px] items-center gap-2.5 pr-2.5 pl-3.5 text-item-ui">
        <Popover>
          <PopoverTrigger asChild>
            <button
              type="button"
              aria-label={fillCopy(t.recolor, { name: label.name })}
              className="grid size-6 shrink-0 cursor-pointer place-items-center rounded-[6px] border-0 bg-transparent p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              <span
                aria-hidden="true"
                className={cn(
                  "size-4 rounded-[5px]",
                  ITEM_LABEL_TONE_CLASS[tone],
                )}
              />
            </button>
          </PopoverTrigger>
          <PopoverContent align="start" className="w-auto p-2">
            <ColorChoices
              label={t.colorLabel}
              current={label.color}
              onPick={(color) =>
                void run(() => panel.onUpdate(label.id, { color }), scope)
              }
            />
          </PopoverContent>
        </Popover>
        {renaming ? (
          <input
            autoFocus
            value={name}
            aria-label={t.nameLabel}
            {...(duplicate
              ? { "aria-invalid": true, "aria-describedby": `${errorId}-dup` }
              : failed)}
            onChange={(event) => setName(event.target.value)}
            onBlur={() => void commit()}
            onKeyDown={(event) => {
              if (event.key === "Enter") event.currentTarget.blur();
              if (event.key === "Escape") {
                event.stopPropagation();
                setName(label.name);
                setRenaming(false);
              }
            }}
            className={cn(INPUT_CLASS, "h-8 min-w-0 flex-1")}
          />
        ) : (
          <span
            className={cn(
              "inline-flex h-5.5 max-w-full items-center truncate rounded-[6px] px-2 text-micro font-semibold",
              ITEM_LABEL_TONE_CLASS[tone],
            )}
          >
            {label.name || label.color}
          </span>
        )}
        <span className="flex-1" />
        {count === undefined ? null : (
          <span className="text-crumb text-meta-foreground">
            {plural(t.onItems_one, t.onItems_other, count)}
          </span>
        )}
        <button
          type="button"
          aria-label={fillCopy(t.rename, { name: label.name })}
          onClick={() => {
            setName(label.name);
            setRenaming(true);
          }}
          className={ICON_BUTTON_CLASS}
        >
          <Pencil aria-hidden="true" />
        </button>
        <button
          type="button"
          aria-label={fillCopy(t.delete, { name: label.name })}
          aria-expanded={deleting}
          onClick={onDelete}
          className={ICON_BUTTON_CLASS}
        >
          <Trash2 aria-hidden="true" />
        </button>
      </div>
      <div className="px-3.5 empty:hidden">
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
        <div
          role="group"
          aria-label={fillCopy(t.deleteQuestion, { name: label.name })}
          className="flex flex-col gap-2.5 border-t border-line bg-background px-3.5 pt-3 pb-3.5"
        >
          <p className="m-0 text-item-small text-muted-foreground">
            <b className="font-semibold text-foreground">
              {fillCopy(t.deleteQuestion, { name: label.name })}
            </b>{" "}
            {count === undefined
              ? t.deleteBodyUnknown
              : count === 0
                ? t.deleteBodyNone
                : plural(t.deleteBody_one, t.deleteBody_other, count)}
          </p>
          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={onDeleteDone}
              className={BUTTON_CLASS.outline}
            >
              {copy.common.cancel}
            </button>
            <button
              type="button"
              onClick={() => void confirmDelete()}
              className={BUTTON_CLASS.danger}
            >
              {t.deleteConfirm}
            </button>
          </div>
        </div>
      ) : null}
    </li>
  );
}
