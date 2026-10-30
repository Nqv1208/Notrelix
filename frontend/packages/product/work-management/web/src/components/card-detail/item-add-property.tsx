import { useId, useState } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { ITEM_LABEL_COLORS } from "../shared/item-display";
import { fillSheetCopy, type ItemSheetCopy } from "./item-sheet.copy";
import type { ItemSheetNewField } from "./task-detail-types";

const TYPES = [
  "text",
  "number",
  "checkbox",
  "select",
  "multi_select",
] as const satisfies readonly ItemSheetNewField["fieldType"][];

const OPTION_COLORS = Object.values(ITEM_LABEL_COLORS);

const INPUT_CLASS =
  "box-border h-8 w-full rounded-md border border-line-strong bg-popover px-2 text-sm font-normal text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger";

/**
 * "+ Thêm thuộc tính": lists the properties of the board that hold nothing for
 * this item (a pick shows that property's value control) and a "Thuộc tính
 * mới" row that grows into a name, type and choices form in place. The trigger
 * is passed in; the control is only rendered for people who may create fields.
 */
export function AddPropertyPopover({
  t,
  emptyFields,
  onReveal,
  onCreate,
  onCreated,
  manageHref,
  triggerClassName,
  triggerLabel,
  initialName,
}: {
  /** Classes of the "+" trigger the popover renders itself. */
  triggerClassName: string;
  /** Words beside the "+"; absent: the icon alone. */
  triggerLabel?: string;
  /** A name already typed (the popup menu's "Tạo trường “…”"): the panel opens on the new-field form with it. */
  initialName?: string;
  t: ItemSheetCopy["properties"];
  /** Custom properties with no value on this item. */
  emptyFields: readonly FieldDefinition[];
  onReveal: (fieldId: string) => void;
  onCreate: (input: ItemSheetNewField) => Promise<string | null>;
  /** The new property exists: the sheet asks for its value right away. */
  onCreated: (fieldId: string) => void;
  /** Board settings › Fields; absent: no "Quản lý trường" link. */
  manageHref?: string;
}) {
  const [open, setOpen] = useState(initialName !== undefined);
  const [creating, setCreating] = useState(initialName !== undefined);
  const [name, setName] = useState(initialName ?? "");
  const [type, setType] = useState<(typeof TYPES)[number]>("select");
  const [choices, setChoices] = useState(["", ""]);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const errorId = useId();
  const nameId = useId();
  const typeId = useId();
  const hasChoices = type === "select" || type === "multi_select";
  const filled = choices.map((choice) => choice.trim()).filter(Boolean);
  const canSubmit =
    name.trim() !== "" && (!hasChoices || filled.length > 0) && !busy;

  function reset() {
    setCreating(false);
    setName("");
    setType("select");
    setChoices(["", ""]);
    setFailed(false);
  }

  async function submit() {
    if (!canSubmit) return;
    setBusy(true);
    setFailed(false);
    const id = await onCreate({
      name: name.trim(),
      fieldType: type,
      options: hasChoices
        ? filled.map((choice, index) => ({
            name: choice,
            color: OPTION_COLORS[index % OPTION_COLORS.length] ?? "",
          }))
        : [],
    }).catch(() => null);
    setBusy(false);
    if (id) {
      setOpen(false);
      reset();
      onCreated(id);
    } else setFailed(true);
  }

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) reset();
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={t.addProperty}
          title={t.addProperty}
          className={triggerClassName}
        >
          <Plus aria-hidden="true" className="size-4" />
          {triggerLabel}
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-72 p-1">
        {creating ? (
          <form
            className="m-1 flex flex-col gap-2.5 rounded-lg bg-state-selected p-2.5"
            onSubmit={(event) => {
              event.preventDefault();
              void submit();
            }}
          >
            <label
              htmlFor={nameId}
              className="text-xs font-semibold text-meta-foreground"
            >
              {t.propertyName}
            </label>
            <input
              id={nameId}
              type="text"
              autoFocus
              value={name}
              maxLength={100}
              aria-invalid={failed ? true : undefined}
              aria-describedby={failed ? errorId : undefined}
              onChange={(event) => {
                setName(event.target.value);
                setFailed(false);
              }}
              className={INPUT_CLASS}
            />
            <label
              htmlFor={typeId}
              className="text-xs font-semibold text-meta-foreground"
            >
              {t.propertyType}
            </label>
            <select
              id={typeId}
              value={type}
              onChange={(event) =>
                setType(event.target.value as (typeof TYPES)[number])
              }
              className={INPUT_CLASS}
            >
              {TYPES.map((value) => (
                <option key={value} value={value}>
                  {t.propertyTypes[value]}
                </option>
              ))}
            </select>
            {hasChoices ? (
              <fieldset className="m-0 flex min-w-0 flex-col gap-1.5 border-0 p-0">
                <legend className="mb-1 p-0 text-xs font-semibold text-meta-foreground">
                  {t.propertyOptions}
                </legend>
                {choices.map((choice, index) => (
                  <input
                    // The list only grows, so the index is a stable key.
                    key={index}
                    type="text"
                    value={choice}
                    maxLength={100}
                    aria-label={fillSheetCopy(t.propertyOption, {
                      index: index + 1,
                    })}
                    placeholder={fillSheetCopy(t.propertyOption, {
                      index: index + 1,
                    })}
                    onChange={(event) =>
                      setChoices((list) =>
                        list.map((entry, at) =>
                          at === index ? event.target.value : entry,
                        ),
                      )
                    }
                    className={INPUT_CLASS}
                  />
                ))}
                <button
                  type="button"
                  onClick={() => setChoices((list) => [...list, ""])}
                  className="flex h-8 cursor-pointer items-center gap-2 rounded-md border border-dashed border-line-strong bg-transparent px-2 text-sm font-medium text-brand-subtle-foreground"
                >
                  <Plus aria-hidden="true" className="size-3.5" />
                  {t.addOption}
                </button>
              </fieldset>
            ) : null}
            {failed ? (
              <p
                id={errorId}
                role="alert"
                className="m-0 flex flex-wrap items-center gap-x-2 text-xs text-danger-text"
              >
                {t.createPropertyFailed}
              </p>
            ) : null}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={reset}
                className="h-7 cursor-pointer rounded-md border-0 bg-transparent px-2.5 text-sm text-muted-foreground hover:bg-state-hover"
              >
                {t.cancel}
              </button>
              <button
                type="submit"
                disabled={!canSubmit}
                className="h-7 cursor-pointer rounded-md border-0 bg-primary px-2.5 text-sm font-semibold text-primary-foreground disabled:cursor-default disabled:opacity-50"
              >
                {failed ? t.createLabelRetry : t.createProperty}
              </button>
            </div>
          </form>
        ) : (
          <ul aria-label={t.addProperty} className="flex flex-col gap-0.5">
            {emptyFields.map((field) => (
              <li key={field.id}>
                <button
                  type="button"
                  onClick={() => {
                    setOpen(false);
                    onReveal(field.id);
                  }}
                  className="flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 py-1.5 text-left text-sm hover:bg-state-hover"
                >
                  <span className="min-w-0 flex-1 truncate">{field.name}</span>
                  <span className="text-xs text-meta-foreground">
                    {t.setValueFor}
                  </span>
                </button>
              </li>
            ))}
            <li>
              <button
                type="button"
                onClick={() => setCreating(true)}
                className="flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 py-1.5 text-left text-sm font-medium text-brand-subtle-foreground hover:bg-state-hover"
              >
                <Plus aria-hidden="true" className="size-3.5 shrink-0" />
                {t.newProperty}
              </button>
            </li>
          </ul>
        )}
        {manageHref && !creating ? (
          <a
            href={manageHref}
            className="mt-0.5 block border-t border-line px-2 pt-2 pb-1 text-xs font-medium text-brand-subtle-foreground"
          >
            {t.manageFields}
          </a>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
