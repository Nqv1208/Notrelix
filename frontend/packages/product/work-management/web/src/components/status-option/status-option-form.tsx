import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import {
  Button,
  Input,
  RadioGroup,
  RadioGroupItem,
  cn,
} from "@notrelix/ui-web";
import { fillCopy } from "../views/kanban/kanban-card.copy";
import {
  DEFAULT_STATUS_OPTION_CATEGORY,
  DEFAULT_STATUS_OPTION_COLOR_BY_CATEGORY,
  STATUS_OPTION_CATEGORIES,
  STATUS_OPTION_COLORS,
  type StatusOptionCategory,
} from "./status-option-palette";
import type { StatusOptionFormCopy } from "./status-option-form.copy";

/** What the form hands back: a named, coloured (and categorised) status. */
export interface StatusOptionDraft {
  readonly name: string;
  /** A palette value (the stored colour). */
  readonly color: string;
  /** The status group; `undefined` when the form shows no group (a Select field). */
  readonly category?: StatusOptionCategory;
}

export interface StatusOptionFormProps {
  copy: StatusOptionFormCopy;
  /** "create" adds a status, "edit" changes one (rename, recolour, regroup). */
  mode?: "create" | "edit";
  /** Values the form starts with (edit mode, or a suggested name). */
  initial?: Partial<StatusOptionDraft>;
  /**
   * Names already taken on the field. In edit mode leave the option's own
   * name out so keeping it is not a duplicate. Compared trimmed and
   * case-insensitively.
   */
  existingNames: readonly string[];
  /**
   * What is being named: a status of the Status field (asks for its group) or
   * a value of another Select field (name and colour only, "value" wording).
   */
  subject?: "status" | "value";
  /** Whether the status group is asked; follows `subject` (status yes, value no). */
  showCategory?: boolean;
  /** A write is in flight: the form keeps its values and cannot be sent again. */
  pending?: boolean;
  onSubmit: (draft: StatusOptionDraft) => void;
  onCancel: () => void;
}

const MAX_NAME_LENGTH = 200;

function normalised(name: string): string {
  return name.trim().toLocaleLowerCase();
}

/**
 * The status option form: name, colour from the palette and (for a Status
 * field) the group. Enter sends, Escape cancels, an empty or duplicate name
 * is refused with a message and nothing is sent. Pure: the container creates
 * or updates the option and decides where the form is shown (a popover on
 * the Kanban, a menu in the Table).
 */
export function StatusOptionForm({
  copy,
  mode = "create",
  initial,
  existingNames,
  subject = "status",
  showCategory: showCategoryProp,
  pending = false,
  onSubmit,
  onCancel,
}: StatusOptionFormProps) {
  const showCategory = showCategoryProp ?? subject === "status";
  const initialCategory = initial?.category ?? DEFAULT_STATUS_OPTION_CATEGORY;
  const [name, setName] = useState(initial?.name ?? "");
  const [category, setCategory] =
    useState<StatusOptionCategory>(initialCategory);
  const [color, setColor] = useState(
    initial?.color ?? DEFAULT_STATUS_OPTION_COLOR_BY_CATEGORY[initialCategory],
  );
  // A colour the user did not pick follows the category until they choose one.
  const colorChosen = useRef(initial?.color !== undefined);
  const [attempted, setAttempted] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const ids = useId();
  const errorId = `${ids}-error`;

  useEffect(() => {
    inputRef.current?.focus();
    inputRef.current?.select();
  }, []);

  const trimmed = name.trim();
  const duplicate =
    trimmed !== "" &&
    existingNames.some(
      (existing) => normalised(existing) === normalised(trimmed),
    );
  const forValue = subject === "value";
  const error =
    trimmed.length > MAX_NAME_LENGTH
      ? copy.errorTooLong
      : duplicate
        ? fillCopy(forValue ? copy.valueErrorDuplicate : copy.errorDuplicate, {
            name: trimmed,
          })
        : attempted && trimmed === ""
          ? forValue
            ? copy.valueErrorEmpty
            : copy.errorEmpty
          : null;
  const valid =
    trimmed !== "" && !duplicate && trimmed.length <= MAX_NAME_LENGTH;

  function submit(event: FormEvent) {
    event.preventDefault();
    if (pending) return;
    setAttempted(true);
    if (!valid) {
      inputRef.current?.focus();
      return;
    }
    onSubmit({
      name: trimmed,
      color,
      ...(showCategory ? { category } : {}),
    });
  }

  function chooseCategory(next: StatusOptionCategory) {
    setCategory(next);
    if (!colorChosen.current) {
      setColor(DEFAULT_STATUS_OPTION_COLOR_BY_CATEGORY[next]);
    }
  }

  return (
    <form
      data-status-option-form=""
      className="flex flex-col gap-3"
      onSubmit={submit}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.stopPropagation();
          onCancel();
        }
      }}
      noValidate
    >
      <h3 className="text-nav font-semibold text-foreground">
        {mode === "edit"
          ? forValue
            ? copy.valueTitleEdit
            : copy.titleEdit
          : forValue
            ? copy.valueTitleCreate
            : copy.titleCreate}
      </h3>

      <div className="flex flex-col gap-1.5">
        <label
          htmlFor={`${ids}-name`}
          className="text-metadata font-medium text-meta-foreground"
        >
          {forValue ? copy.valueNameLabel : copy.nameLabel}
        </label>
        <Input
          ref={inputRef}
          id={`${ids}-name`}
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder={
            forValue ? copy.valueNamePlaceholder : copy.namePlaceholder
          }
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          autoComplete="off"
          className="h-8 text-label"
        />
        {error ? (
          <p
            id={errorId}
            role="alert"
            className="text-metadata text-danger-text"
          >
            {error}
          </p>
        ) : null}
      </div>

      <div className="flex flex-col gap-1.5">
        <span
          id={`${ids}-color`}
          className="text-metadata font-medium text-meta-foreground"
        >
          {copy.colorLabel}
        </span>
        <RadioGroup
          aria-labelledby={`${ids}-color`}
          value={color}
          onValueChange={(next) => {
            colorChosen.current = true;
            setColor(next);
          }}
          className="flex w-full flex-wrap gap-2"
        >
          {STATUS_OPTION_COLORS.map((option) => (
            <RadioGroupItem
              key={option.id}
              value={option.value}
              aria-label={copy[`color_${option.id}`]}
              style={{ backgroundColor: option.value }}
              className="size-6 border-transparent shadow-none ring-offset-2 ring-offset-popover aria-checked:ring-2 aria-checked:ring-foreground dark:bg-transparent [&_[data-slot=radio-group-indicator]]:size-6 [&_[data-slot=radio-group-indicator]]:text-white"
            />
          ))}
        </RadioGroup>
      </div>

      {showCategory ? (
        <div className="flex flex-col gap-1.5">
          <span
            id={`${ids}-category`}
            className="text-metadata font-medium text-meta-foreground"
          >
            {copy.categoryLabel}
          </span>
          <RadioGroup
            aria-labelledby={`${ids}-category`}
            value={category}
            onValueChange={(next) =>
              chooseCategory(next as StatusOptionCategory)
            }
            className="flex w-full flex-col gap-1.5"
          >
            {STATUS_OPTION_CATEGORIES.map((option) => (
              <label
                key={option}
                className={cn(
                  "flex h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-label text-foreground hover:bg-state-hover",
                  category === option && "bg-state-hover",
                )}
              >
                <RadioGroupItem value={option} />
                {copy[`category_${option}`]}
              </label>
            ))}
          </RadioGroup>
        </div>
      ) : null}

      <div className="flex items-center justify-end gap-1.5">
        <Button type="button" variant="ghost" size="sm" onClick={onCancel}>
          {copy.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending
            ? copy.saving
            : mode === "edit"
              ? copy.submitEdit
              : forValue
                ? copy.valueSubmitCreate
                : copy.submitCreate}
        </Button>
      </div>
    </form>
  );
}
