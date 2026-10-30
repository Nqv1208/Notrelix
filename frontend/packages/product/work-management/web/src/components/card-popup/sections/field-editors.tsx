import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import {
  AlertCircle,
  Calendar as CalendarIcon,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Hash,
  Loader2,
  Minus,
  Sigma,
  Type,
  X,
} from "lucide-react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import {
  fieldTypeRegistry,
  priorityFromOptionName,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import { PriorityGlyph } from "../../shared/priority-glyph";

/** The popup's priority shapes, coloured as the artboard has them (medium in the "doing" blue). */
const LEVEL_TONE = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-work-status-doing",
  low: "text-meta-foreground",
} as const;
import { describeComputed } from "../../card-detail/item-field-extra-editors";
import {
  isFieldValueEmpty,
  multiValueIds,
} from "../../card-detail/item-field-more-editors";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup, useCanEdit } from "../card-popup-context";
import {
  ChipBtn,
  ChipReadOnly,
  IconBtn,
  PanelBtn,
  PanelHead,
  PanelInput,
  PanelPopover,
} from "../popup-ui";

const CHEVRON = (
  <ChevronDown
    aria-hidden="true"
    className="ml-0.5 size-[13px] text-meta-foreground"
  />
);
const CLOSE_ICONS = {
  back: null,
  close: <X aria-hidden="true" className="size-[13px]" />,
};

/** `.frv`: the value of a field row, drawn as plain text until it is hovered or edited, 8 px wider than its column so the text lines up with the label's column. */
const CTL =
  "box-border flex h-11 md:h-8 w-[calc(100%+16px)] -ml-2 items-center gap-2 rounded-[8px] border border-transparent bg-transparent px-2 text-left text-[14px] text-foreground hover:bg-state-hover-strong disabled:cursor-not-allowed focus-within:border-primary focus-within:bg-popover focus-within:ring-[3px] focus-within:ring-brand-subtle focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";
const CTL_RO =
  "box-border flex h-11 md:h-8 w-[calc(100%+16px)] -ml-2 items-center gap-2 rounded-[8px] border border-transparent bg-transparent px-2 text-[14px] text-muted-foreground";

/** `dd/mm/yyyy` of a `yyyy-mm-dd` value; "" when it is not a day. */
export function dayText(value: unknown): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(
    typeof value === "string" ? value : "",
  );
  return m ? `${m[3]}/${m[2]}/${m[1]}` : "";
}

function parseDay(text: string): string | null {
  const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(text.trim());
  if (!m) return null;
  const [d, mo, y] = [Number(m[1]), Number(m[2]), Number(m[3])];
  const date = new Date(Date.UTC(y, mo - 1, d));
  if (
    date.getUTCFullYear() !== y ||
    date.getUTCMonth() !== mo - 1 ||
    date.getUTCDate() !== d
  )
    return null;
  return `${y}-${String(mo).padStart(2, "0")}-${String(d).padStart(2, "0")}`;
}

/** The month grid of the date panel: year and month steps, Monday first. */
function MiniCalendar({
  value,
  today,
  onPick,
}: {
  value: string | null;
  today: Date;
  onPick: (day: string) => void;
}) {
  const { copy } = useCardPopup();
  const t = copy.sections.chips;
  const seed = value ? new Date(`${value}T00:00:00Z`) : today;
  const [view, setView] = useState({
    y: seed.getUTCFullYear?.() ?? today.getFullYear(),
    m: Number.isNaN(seed.getTime())
      ? today.getMonth()
      : value
        ? seed.getUTCMonth()
        : today.getMonth(),
  });
  const step = (months: number) => {
    const d = new Date(Date.UTC(view.y, view.m + months, 1));
    setView({ y: d.getUTCFullYear(), m: d.getUTCMonth() });
  };
  const first = new Date(Date.UTC(view.y, view.m, 1));
  const offset = (first.getUTCDay() + 6) % 7;
  const days = new Date(Date.UTC(view.y, view.m + 1, 0)).getUTCDate();
  const cells = Math.ceil((offset + days) / 7) * 7;
  const iso = (d: Date) => d.toISOString().slice(0, 10);
  const todayIso = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
  const month = new Intl.DateTimeFormat(copy.locale, {
    month: "long",
    timeZone: "UTC",
  }).format(first);
  return (
    <div>
      <div className="mb-1 flex items-center justify-between">
        <IconBtn small aria-label={t.previousYear} onClick={() => step(-12)}>
          <ChevronsLeft aria-hidden="true" className="size-4" />
        </IconBtn>
        <IconBtn small aria-label={t.previousMonth} onClick={() => step(-1)}>
          <ChevronLeft aria-hidden="true" className="size-4" />
        </IconBtn>
        <b className="text-[14px]" aria-live="polite">
          {fillPopupCopy(t.monthTitle, {
            month: month.charAt(0).toUpperCase() + month.slice(1),
            year: view.y,
          })}
        </b>
        <IconBtn small aria-label={t.nextMonth} onClick={() => step(1)}>
          <ChevronRight aria-hidden="true" className="size-4" />
        </IconBtn>
        <IconBtn small aria-label={t.nextYear} onClick={() => step(12)}>
          <ChevronsRight aria-hidden="true" className="size-4" />
        </IconBtn>
      </div>
      <div className="grid grid-cols-7 text-center">
        {t.weekdays.split(",").map((w) => (
          <span
            key={w}
            className="grid h-6 place-items-center text-[12px] font-semibold text-muted-foreground"
          >
            {w}
          </span>
        ))}
        {Array.from({ length: cells }, (_, i) => {
          const d = new Date(Date.UTC(view.y, view.m, 1 - offset + i));
          const day = iso(d);
          const out = d.getUTCMonth() !== view.m;
          const selected = day === value;
          return (
            <button
              key={day}
              type="button"
              aria-label={new Intl.DateTimeFormat(copy.locale, {
                dateStyle: "long",
                timeZone: "UTC",
              }).format(d)}
              aria-pressed={selected}
              onClick={() => onPick(day)}
              className={cn(
                "relative grid h-[34px] place-items-center rounded-md border-0 bg-transparent p-0 text-[13.5px] text-foreground hover:bg-state-hover-strong",
                out && "text-meta-foreground",
                selected &&
                  "bg-primary font-bold text-primary-foreground hover:bg-primary",
                day === todayIso &&
                  "after:absolute after:right-2 after:bottom-[3px] after:left-2 after:h-0.5 after:rounded-[1px] after:bg-primary",
                day === todayIso && selected && "after:bg-primary-foreground",
              )}
            >
              {d.getUTCDate()}
            </button>
          );
        })}
      </div>
    </div>
  );
}

/** The date of a date field: a small calendar, a typed day, Lưu and Xoá giá trị. */
function DateEditor({
  field,
  value,
  onSave,
  onClose,
}: {
  field: FieldDefinition;
  value: string | null;
  onSave: (v: string | null) => void;
  onClose: () => void;
}) {
  const { copy, now } = useCardPopup();
  const t = copy.sections.chips;
  const id = useId();
  const [text, setText] = useState(dayText(value));
  const parsed = parseDay(text);
  const invalid = text.trim() !== "" && parsed === null;
  return (
    <>
      <PanelHead
        title={field.name}
        onClose={onClose}
        closeLabel={t.close}
        icons={CLOSE_ICONS}
      />
      <MiniCalendar
        value={parsed ?? value}
        today={now}
        onPick={(day) => setText(dayText(day))}
      />
      <label
        htmlFor={id}
        className="mt-2.5 mb-1.5 block text-[12px] font-semibold text-muted-foreground"
      >
        {t.dayField}
      </label>
      <PanelInput
        id={id}
        value={text}
        placeholder={t.dayPlaceholder}
        invalid={invalid}
        aria-invalid={invalid || undefined}
        onChange={(e) => setText(e.target.value)}
      />
      {invalid ? (
        <p
          role="alert"
          className="m-0 mt-1.5 flex items-start gap-1.5 text-[12.5px] font-medium text-danger-text"
        >
          <AlertCircle aria-hidden="true" className="size-4 shrink-0" />
          {t.invalidDate}
        </p>
      ) : null}
      <div className="mt-3 flex gap-2">
        <PanelBtn
          variant="primary"
          className="flex-1"
          disabled={invalid || parsed === null}
          onClick={() => {
            if (parsed) onSave(parsed);
            onClose();
          }}
        >
          {t.save}
        </PanelBtn>
        <PanelBtn
          className="flex-1"
          onClick={() => {
            onSave(null);
            onClose();
          }}
        >
          {t.clearValue}
        </PanelBtn>
      </div>
    </>
  );
}

const optionDot = (color?: string) => (
  <span
    aria-hidden="true"
    className="size-[9px] shrink-0 rounded-full bg-meta-foreground"
    style={color ? { backgroundColor: color } : undefined}
  />
);

const PRIORITY_OPT =
  "flex h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

const OPT =
  "flex min-h-9 w-full items-center gap-2.5 rounded-lg border-0 bg-transparent px-2.5 text-left text-[14px] text-foreground hover:bg-state-hover-strong";

/** The write of one field, tracked under its own key so the chip and the section show its state. */
export function useFieldWrite(field: FieldDefinition) {
  const { card, callbacks, track, saves } = useCardPopup();
  return {
    save: saves[`field:${field.id}`],
    write: (value: unknown) =>
      track(`field:${field.id}`, async () =>
        callbacks.onUpdateFieldValue({
          cardId: card.id,
          fieldDefinitionId: field.id,
          value,
        }),
      ),
  };
}

/** Save and error line under a text-like control. */
function Feedback({
  field,
  invalid,
}: {
  field: FieldDefinition;
  invalid?: string;
}) {
  const { copy } = useCardPopup();
  const t = copy.sections.chips;
  const { save } = useFieldWrite(field);
  if (invalid)
    return (
      <p
        role="alert"
        className="m-0 flex items-start gap-1.5 text-[12.5px] font-medium text-danger-text"
      >
        <AlertCircle aria-hidden="true" className="size-4 shrink-0" />
        {invalid}
      </p>
    );
  if (save?.status === "error")
    return (
      <p
        role="alert"
        className="m-0 flex items-center gap-2 text-[12.5px] font-medium text-danger-text"
      >
        <AlertCircle aria-hidden="true" className="size-4 shrink-0" />
        {fillPopupCopy(t.saveFailed, { name: field.name })}
        <PanelBtn small onClick={save.retry}>
          {t.retry}
        </PanelBtn>
      </p>
    );
  return null;
}

/**
 * One field's value with its editor. `chip` is the compact control of the chip
 * row, `ctl` the full-width control of the fields section. Computed fields and
 * a read-only card show the value without an editor.
 */
export function FieldControl({
  field,
  variant,
}: {
  field: FieldDefinition;
  variant: "chip" | "ctl";
}) {
  const { card, copy, fields, openFieldId, clearOpenField } = useCardPopup();
  const priorityId = fields.priority?.id;
  const t = copy.sections.chips;
  const f = copy.sections.fields;
  const sheetCopy = useItemSheetCopy();
  const canEdit = useCanEdit();
  const { write, save } = useFieldWrite(field);
  const [open, setOpen] = useState(false);
  // "+ Thêm" added this field: its picker opens at once (a typed control takes the cursor itself).
  useEffect(() => {
    if (openFieldId !== field.id) return;
    if (["select", "multi_select", "date"].includes(field.fieldType)) {
      setOpen(true);
      clearOpenField();
    }
  }, [openFieldId, field.id, field.fieldType, clearOpenField]);
  const raw = card.fieldValues[field.id];
  const chip = variant === "chip";
  const saving = save?.status === "saving";
  const failed = save?.status === "error";
  const label = field.name;

  const trigger = (
    content: ReactNode,
    opts: { tone?: "on" | "ok"; haspopup?: "listbox" | "dialog" } = {},
  ) => {
    const body = <>{content}</>;
    if (!canEdit)
      return chip ? (
        <ChipReadOnly
          className={
            opts.tone === "ok"
              ? "border-transparent border-solid bg-success-subtle font-semibold text-success-subtle-foreground"
              : undefined
          }
          title={fillPopupCopy(t.readOnlyChip, { name: label })}
        >
          {body}
        </ChipReadOnly>
      ) : (
        <span className={CTL_RO}>{body}</span>
      );
    return (
      <PopoverTrigger asChild>
        {chip ? (
          <ChipBtn
            aria-haspopup={opts.haspopup}
            aria-expanded={open}
            tone={opts.tone}
            className={failed ? "border-danger" : undefined}
          >
            {body}
          </ChipBtn>
        ) : (
          <button
            type="button"
            aria-haspopup={opts.haspopup}
            aria-expanded={open}
            className={cn(CTL, failed && "border-danger")}
          >
            {body}
          </button>
        )}
      </PopoverTrigger>
    );
  };
  const caret = canEdit ? CHEVRON : null;
  const spin = saving ? (
    <Loader2
      aria-hidden="true"
      className="size-[13px] animate-spin text-meta-foreground"
    />
  ) : null;
  const valueClass = cn(
    "min-w-0 flex-1 truncate",
    !chip && isFieldValueEmpty(raw) && "text-meta-foreground",
  );
  const wrapChip = (children: ReactNode) =>
    chip ? children : <span className={valueClass}>{children}</span>;
  const tail = chip ? (
    caret
  ) : (
    <>
      {spin}
      {caret}
    </>
  );

  // Computed (Formula, Rollup): read-only.
  if (fieldTypeRegistry[field.fieldType]?.computed) {
    const info = describeComputed(
      field,
      card.computedValues?.[field.id],
      sheetCopy.properties.computed,
    );
    return chip ? (
      <ChipReadOnly title={info.title}>
        <Sigma aria-hidden="true" className="size-[13px]" />
        {info.text}
      </ChipReadOnly>
    ) : (
      <span className={CTL_RO} title={info.title}>
        <Sigma aria-hidden="true" className="size-[13px]" />
        <span className="truncate">{info.text}</span>
      </span>
    );
  }

  if (field.fieldType === "checkbox") {
    const on = raw === true;
    const text = on ? t.checkboxOn : t.checkboxOff;
    const box = (
      <span
        aria-hidden="true"
        className={cn(
          "grid size-4 shrink-0 place-items-center rounded-[5px] border-[1.5px] text-primary-foreground",
          on ? "border-primary bg-primary" : "border-line-strong bg-popover",
        )}
      >
        {on ? <Check className="size-3" strokeWidth={3} /> : null}
      </span>
    );
    if (chip)
      return (
        <ChipBtn
          role="checkbox"
          aria-checked={on}
          aria-label={label}
          disabled={!canEdit}
          tone={on ? "ok" : undefined}
          onClick={() => write(!on)}
          className={cn(
            !canEdit &&
              "cursor-default border-dashed bg-background text-muted-foreground hover:bg-background",
          )}
        >
          {on ? (
            <Check aria-hidden="true" className="size-[13px]" strokeWidth={3} />
          ) : (
            box
          )}
          {text}
        </ChipBtn>
      );
    return (
      <label className={canEdit ? CTL : CTL_RO}>
        <input
          type="checkbox"
          className="size-[18px] shrink-0 accent-primary"
          checked={on}
          disabled={!canEdit}
          aria-label={label}
          onChange={() => write(!on)}
        />
        <span>{on ? t.checkboxOn : t.checkboxOff}</span>
      </label>
    );
  }

  if (field.fieldType === "date") {
    const text = dayText(raw);
    return (
      <PanelPopover
        open={open}
        onOpenChange={setOpen}
        width={312}
        trigger={trigger(
          <>
            <CalendarIcon aria-hidden="true" className="size-[13px]" />
            {wrapChip(text || (chip ? t.notSet : f.selectEmpty))}
            {tail}
          </>,
          { haspopup: "dialog" },
        )}
      >
        {open ? (
          <DateEditor
            field={field}
            value={typeof raw === "string" ? raw.slice(0, 10) : null}
            onSave={write}
            onClose={() => setOpen(false)}
          />
        ) : null}
      </PanelPopover>
    );
  }

  if (field.fieldType === "select" && field.id === priorityId) {
    const glyph = (name: string | undefined) => {
      const level = name ? priorityFromOptionName(name) : undefined;
      return level ? (
        <PriorityGlyph
          level={level}
          className={cn("size-4", LEVEL_TONE[level])}
        />
      ) : (
        <Minus aria-hidden="true" className="size-4 text-meta-foreground" />
      );
    };
    const current = field.options.find((o) => o.id === raw);
    return (
      <PanelPopover
        open={open}
        onOpenChange={setOpen}
        width={220}
        className="p-1.5"
        trigger={trigger(
          <>
            {glyph(current?.label)}
            {wrapChip(current?.label ?? t.noPriority)}
          </>,
          { haspopup: "listbox" },
        )}
      >
        <div role="listbox" aria-label={field.name}>
          {field.options.map((o) => (
            <button
              key={o.id}
              type="button"
              role="option"
              aria-selected={o.id === raw}
              onClick={() => {
                write(o.id);
                setOpen(false);
              }}
              className={cn(PRIORITY_OPT, o.id === raw && "font-semibold")}
            >
              {glyph(o.label)}
              {o.label}
              {o.id === raw ? (
                <Check
                  aria-hidden="true"
                  className="ml-auto size-[13px] text-brand-ink"
                />
              ) : null}
            </button>
          ))}
          <div className="-mx-1.5 my-1.5 h-px bg-line" />
          <button
            type="button"
            role="option"
            aria-selected={!current}
            className={cn(PRIORITY_OPT, !current && "font-semibold")}
            onClick={() => {
              write(null);
              setOpen(false);
            }}
          >
            {glyph(undefined)}
            {t.noPriority}
            {!current ? (
              <Check
                aria-hidden="true"
                className="ml-auto size-[13px] text-brand-ink"
              />
            ) : null}
          </button>
        </div>
      </PanelPopover>
    );
  }

  if (field.fieldType === "select") {
    const current = field.options.find((o) => o.id === raw);
    return (
      <PanelPopover
        open={open}
        onOpenChange={setOpen}
        width={240}
        trigger={trigger(
          <>
            {current ? optionDot(current.color) : null}
            {wrapChip(current?.label ?? (chip ? t.notSet : f.selectEmpty))}
            {tail}
          </>,
          { haspopup: "listbox" },
        )}
      >
        <PanelHead
          title={field.name}
          onClose={() => setOpen(false)}
          closeLabel={t.close}
          icons={CLOSE_ICONS}
        />
        <div role="listbox" aria-label={field.name}>
          {field.options.map((o) => (
            <button
              key={o.id}
              type="button"
              role="option"
              aria-selected={o.id === raw}
              onClick={() => {
                write(o.id);
                setOpen(false);
              }}
              className={cn(
                OPT,
                o.id === raw &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              {optionDot(o.color)}
              {o.label}
              {o.id === raw ? (
                <Check aria-hidden="true" className="ml-auto size-4" />
              ) : null}
            </button>
          ))}
        </div>
        {raw ? (
          <>
            <div className="-mx-3 my-2 h-px bg-line" />
            <button
              type="button"
              className={OPT}
              onClick={() => {
                write(null);
                setOpen(false);
              }}
            >
              <X aria-hidden="true" className="size-4" />
              {t.clearValue}
            </button>
          </>
        ) : null}
      </PanelPopover>
    );
  }

  if (field.fieldType === "multi_select") {
    const ids = multiValueIds(raw);
    const names = field.options
      .filter((o) => ids.includes(o.id))
      .map((o) => o.label);
    return (
      <PanelPopover
        open={open}
        onOpenChange={setOpen}
        width={240}
        trigger={trigger(
          <>
            {wrapChip(
              names.length ? names.join(", ") : chip ? t.notSet : f.selectEmpty,
            )}
            {tail}
          </>,
          { haspopup: "listbox" },
        )}
      >
        <PanelHead
          title={field.name}
          onClose={() => setOpen(false)}
          closeLabel={t.close}
          icons={CLOSE_ICONS}
        />
        <div role="listbox" aria-multiselectable="true" aria-label={field.name}>
          {field.options.map((o) => {
            const on = ids.includes(o.id);
            return (
              <button
                key={o.id}
                type="button"
                role="option"
                aria-selected={on}
                onClick={() =>
                  write(on ? ids.filter((i) => i !== o.id) : [...ids, o.id])
                }
                className={OPT}
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "grid size-[18px] shrink-0 place-items-center rounded-[5px] border-[1.5px] text-primary-foreground",
                    on ? "border-primary bg-primary" : "border-line-strong",
                  )}
                >
                  {on ? <Check className="size-3" strokeWidth={3} /> : null}
                </span>
                {optionDot(o.color)}
                {o.label}
              </button>
            );
          })}
        </div>
      </PanelPopover>
    );
  }

  // number, text, long_text, link: typed in the control, saved on leaving the box.
  return <TypedControl field={field} raw={raw} chip={chip} />;
}

function TypedControl({
  field,
  raw,
  chip,
}: {
  field: FieldDefinition;
  raw: unknown;
  chip: boolean;
}) {
  const { copy, openFieldId, clearOpenField } = useCardPopup();
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (openFieldId !== field.id) return;
    input.current?.focus();
    clearOpenField();
  }, [openFieldId, field.id, clearOpenField]);
  const t = copy.sections.chips;
  const f = copy.sections.fields;
  const canEdit = useCanEdit();
  const { write, save } = useFieldWrite(field);
  const kind = field.fieldType;
  const initial = raw === undefined || raw === null ? "" : String(raw);
  const [draft, setDraft] = useState(initial);
  const [invalid, setInvalid] = useState<string | undefined>();
  useEffect(() => setDraft(initial), [initial]);
  const saving = save?.status === "saving";
  const Icon = kind === "number" ? Hash : Type;
  const commit = () => {
    if (draft === initial) return setInvalid(undefined);
    if (kind === "number") {
      const parsed = fieldTypeRegistry.number.parseDraft(
        draft.replace(",", "."),
      );
      if (!parsed.ok) return setInvalid(t.invalidNumber);
      setInvalid(undefined);
      return write(parsed.value);
    }
    if (kind === "link") {
      const parsed = fieldTypeRegistry.link.parseDraft(draft);
      if (!parsed.ok) return setInvalid(t.invalidLink);
      setInvalid(undefined);
      return write(parsed.value);
    }
    setInvalid(undefined);
    write(draft === "" ? null : draft);
  };
  const ro = !canEdit;
  return (
    <div className="flex flex-col gap-1.5">
      <label
        className={cn(
          ro ? CTL_RO : CTL,
          chip &&
            "ml-0 h-9 w-48 border-line-strong bg-popover px-3 text-[13.5px]",
          "cursor-text",
          (invalid || save?.status === "error") && "border-danger",
        )}
      >
        <Icon
          aria-hidden="true"
          className="size-[13px] shrink-0 text-muted-foreground"
        />
        <input
          ref={input}
          type="text"
          inputMode={kind === "number" ? "decimal" : undefined}
          value={draft}
          readOnly={ro}
          aria-label={field.name}
          aria-invalid={Boolean(invalid) || undefined}
          placeholder={
            kind === "long_text" || kind === "text" ? f.textEmpty : undefined
          }
          onChange={(e) => setDraft(e.target.value)}
          onBlur={commit}
          onKeyDown={(e) => {
            if (e.key === "Enter") e.currentTarget.blur();
            if (e.key === "Escape" && draft !== initial) {
              e.stopPropagation();
              setDraft(initial);
              setInvalid(undefined);
            }
          }}
          className="min-w-0 flex-1 border-0 bg-transparent p-0 text-[14px] text-foreground outline-none placeholder:text-meta-foreground"
        />
        {saving ? (
          <Loader2
            aria-hidden="true"
            className="size-[13px] animate-spin text-meta-foreground"
          />
        ) : null}
      </label>
      <Feedback field={field} invalid={invalid} />
    </div>
  );
}

/** What the chip row and the section list: every custom field the editor can show. */
export { isFieldValueEmpty };
