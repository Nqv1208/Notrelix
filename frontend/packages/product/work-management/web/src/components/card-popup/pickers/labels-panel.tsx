import { ColourBlindLabelMark } from "../capabilities/colour-blind-label-mark";
import { useMemo, useState } from "react";
import { Check, Pencil } from "lucide-react";
import { useFullBoard } from "@notrelix/work-management-state";
import { cn } from "@notrelix/ui-web";
import type { CardLabel } from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  itemLabelTone,
} from "../../shared/item-display";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCanEdit, useCardPopup } from "../card-popup-context";
import { IconBtn, PanelBtn, PanelInput } from "../popup-ui";
import {
  LABEL_PALETTE,
  paletteNameOf,
  type LabelColorName,
} from "./label-palette";
import {
  Caption,
  FieldLabel,
  Hint,
  PickerHead,
  SearchField,
} from "./picker-bits";

/** `.lbc`: a label as the chip it is on the card. */
function LabelChip({
  label,
  className,
}: {
  label: Pick<CardLabel, "name" | "color">;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex h-8 items-center gap-1.5 rounded-[6px] px-3 text-[13px] font-semibold whitespace-nowrap",
        ITEM_LABEL_TONE_CLASS[itemLabelTone(label)],
        className,
      )}
    >
      <ColourBlindLabelMark label={label} />
      {label.name}
    </span>
  );
}

type View =
  | { readonly kind: "list" }
  | { readonly kind: "create" }
  | { readonly kind: "edit"; readonly label: CardLabel };

/** The name and colour form of "Sửa nhãn" and "Tạo nhãn". */
function LabelForm({
  view,
  usedOn,
  onBack,
  onClose,
}: {
  view: Exclude<View, { kind: "list" }>;
  usedOn: number;
  onBack: () => void;
  onClose: () => void;
}) {
  const { copy, actions, callbacks } = useCardPopup();
  const t = copy.pickers.labels;
  const editing = view.kind === "edit" ? view.label : null;
  const [name, setName] = useState(editing?.name ?? "");
  const [color, setColor] = useState<string>(
    editing?.color ?? LABEL_PALETTE[0][1],
  );
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const [confirmRemove, setConfirmRemove] = useState(false);
  const selected = paletteNameOf(color);
  const trimmed = name.trim();

  const run = async (write: () => Promise<boolean>) => {
    setBusy(true);
    setFailed(false);
    const ok = await write().catch(() => false);
    setBusy(false);
    if (ok) onBack();
    else setFailed(true);
  };
  const save = () =>
    run(() =>
      editing
        ? (actions.updateLabel?.(editing.id, { name: trimmed, color }) ??
          Promise.resolve(false))
        : (callbacks.onCreateLabel?.(trimmed, color) ?? Promise.resolve(false)),
    );

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        if (trimmed && !busy) void save();
      }}
    >
      <PickerHead
        title={editing ? t.editTitle : t.createTitle}
        onBack={onBack}
        onClose={onClose}
      />
      <div className="flex justify-center rounded-[8px] bg-background p-3.5">
        <LabelChip label={{ name: trimmed || t.name, color }} />
      </div>
      <FieldLabel htmlFor="popup-label-name" className="mt-3">
        {t.name}
      </FieldLabel>
      <PanelInput
        id="popup-label-name"
        value={name}
        maxLength={50}
        autoFocus
        onChange={(event) => setName(event.target.value)}
      />
      <Caption id="popup-label-colors">{t.pickColor}</Caption>
      <div
        role="group"
        aria-labelledby="popup-label-colors"
        className="grid grid-cols-5 gap-1.5"
      >
        {LABEL_PALETTE.map(([key, value]) => (
          <button
            key={key}
            type="button"
            aria-label={t.colors[key as LabelColorName]}
            aria-pressed={selected === key}
            onClick={() => setColor(value)}
            style={{ backgroundColor: value }}
            className={cn(
              "relative grid h-8 place-items-center rounded-[6px] border-0 p-0 text-primary-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
              selected === key &&
                "shadow-[0_0_0_2px_var(--popover),0_0_0_4px_var(--primary)]",
            )}
          >
            {selected === key ? (
              <Check aria-hidden="true" className="size-3.5" strokeWidth={3} />
            ) : null}
          </button>
        ))}
      </div>
      <div className="mt-3.5 flex gap-2">
        <PanelBtn
          type="submit"
          variant="primary"
          className="flex-1"
          disabled={!trimmed || busy}
        >
          {editing ? t.save : t.createAction}
        </PanelBtn>
        {editing && actions.removeLabel ? (
          <PanelBtn
            variant="danger"
            disabled={busy}
            onClick={() => {
              if (!confirmRemove) return setConfirmRemove(true);
              void run(() => actions.removeLabel!(editing.id));
            }}
          >
            {confirmRemove ? t.removeConfirm : t.remove}
          </PanelBtn>
        ) : null}
      </div>
      {editing && actions.removeLabel ? (
        <Hint className="mt-2 mr-0.5 ml-0.5">
          {fillPopupCopy(t.removeNote, { count: usedOn })}
        </Hint>
      ) : null}
      {failed ? (
        <Hint error className="mt-2">
          {t.failed}
        </Hint>
      ) : null}
    </form>
  );
}

/** The label picker: search, tick to put a label on the card, edit, create. */
export function LabelsPanel({ onClose }: { onClose: () => void }) {
  const { card, data, copy, callbacks, slots, actions } = useCardPopup();
  const canEdit = useCanEdit();
  const t = copy.pickers.labels;
  const [query, setQuery] = useState("");
  const [view, setView] = useState<View>({ kind: "list" });
  const { groups } = useFullBoard(card.boardId, card.workspaceId);

  const all = data.boardLabels ?? card.labels;
  const on = new Set(card.labels.map((label) => label.id));
  const shown = useMemo(() => {
    const needle = foldForSearch(query);
    return needle
      ? all.filter((label) => foldForSearch(label.name).includes(needle))
      : all;
  }, [all, query]);
  const usedOn = (labelId: string) =>
    groups.reduce(
      (sum, group) =>
        sum +
        group.cards.filter((item) =>
          item.labels.some((label) => label.id === labelId),
        ).length,
      0,
    );

  if (view.kind !== "list")
    return (
      <LabelForm
        view={view}
        usedOn={view.kind === "edit" ? usedOn(view.label.id) : 0}
        onBack={() => setView({ kind: "list" })}
        onClose={onClose}
      />
    );

  return (
    <div>
      <PickerHead title={t.title} onClose={onClose} />
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.searchLabel}
        placeholder={t.search}
      />
      <Caption>{t.boardLabels}</Caption>
      <div className="max-h-[300px] overflow-y-auto">
        {shown.map((label) => (
          <div key={label.id} className="mb-1.5 flex items-center gap-2.5">
            <input
              type="checkbox"
              checked={on.has(label.id)}
              aria-label={fillPopupCopy(t.attach, { name: label.name })}
              onChange={(event) =>
                callbacks.onToggleLabel?.(label.id, event.target.checked)
              }
              className="m-0 size-[18px] shrink-0 accent-primary"
            />
            <LabelChip label={label} className="flex-1" />
            {actions.updateLabel ? (
              <IconBtn
                small
                aria-label={fillPopupCopy(t.edit, { name: label.name })}
                onClick={() => setView({ kind: "edit", label })}
              >
                <Pencil aria-hidden="true" className="size-4" />
              </IconBtn>
            ) : null}
          </div>
        ))}
        {shown.length === 0 ? (
          <Hint className="px-1 py-2">{fillPopupCopy(t.empty, { query })}</Hint>
        ) : null}
      </div>
      {canEdit && callbacks.onCreateLabel ? (
        <PanelBtn
          className="mt-1 w-full"
          onClick={() => setView({ kind: "create" })}
        >
          {t.create}
        </PanelBtn>
      ) : null}
      {slots.labelMarks ? (
        <>
          <div className="-mx-3 my-2 h-px bg-line" />
          {slots.labelMarks}
        </>
      ) : null}
    </div>
  );
}
