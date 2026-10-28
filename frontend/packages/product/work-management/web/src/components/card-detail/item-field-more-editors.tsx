import { useEffect, useState, type ReactNode } from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import type { FieldDefinition } from "@notrelix/work-management-core";

/**
 * The editors of the other custom property types (text, multi select) and the
 * value helpers; the sheet and the item page share them with the select and
 * number editors of `item-field-editors`.
 */

/** A line of text typed into a small popover; Enter or leaving the box saves it. */
export function ItemTextPopover({
  value,
  label,
  readOnly,
  onChange,
  autoOpen = false,
  children,
}: {
  value: string;
  label: string;
  readOnly: boolean;
  onChange: (value: string) => void;
  autoOpen?: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState("");
  useEffect(() => {
    if (autoOpen) setOpen(true);
  }, [autoOpen]);
  if (readOnly) return <>{children}</>;
  const commit = () => {
    if (draft !== value) onChange(draft);
    setOpen(false);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) setDraft(value);
        setOpen(next);
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" aria-label={label} className="w-60 p-2">
        <input
          type="text"
          autoFocus
          value={draft}
          aria-label={label}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            commit();
          }}
          onBlur={commit}
          className="h-8 w-full rounded-[7px] border border-line-strong bg-background px-2 text-item-ui text-foreground"
        />
      </PopoverContent>
    </Popover>
  );
}

/** A MultiSelect field's options as a toggle list; a pick sends the whole set of option ids. */
export function ItemMultiPopover({
  field,
  value,
  readOnly,
  onChange,
  autoOpen = false,
  children,
}: {
  field: FieldDefinition;
  value: readonly string[];
  readOnly: boolean;
  onChange: (value: string[]) => void;
  autoOpen?: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    if (autoOpen) setOpen(true);
  }, [autoOpen]);
  if (readOnly) return <>{children}</>;
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-60 p-1">
        <ul aria-label={field.name} className="flex flex-col gap-0.5">
          {field.options.map((option) => {
            const selected = value.includes(option.id);
            return (
              <li key={option.id}>
                <button
                  type="button"
                  aria-pressed={selected}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-state-hover aria-pressed:bg-state-selected"
                  onClick={() =>
                    onChange(
                      selected
                        ? value.filter((id) => id !== option.id)
                        : [...value, option.id],
                    )
                  }
                >
                  <span
                    aria-hidden="true"
                    className="size-3 shrink-0 rounded-full"
                    style={{ backgroundColor: option.color }}
                  />
                  <span className="min-w-0 flex-1 truncate">
                    {option.label}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      </PopoverContent>
    </Popover>
  );
}

/** Whether a custom field holds nothing for the item: the sheet folds such a field away. */
export function isFieldValueEmpty(raw: unknown): boolean {
  return (
    raw === undefined ||
    raw === null ||
    raw === "" ||
    raw === false ||
    (Array.isArray(raw) && raw.length === 0)
  );
}

/** The option ids a MultiSelect value holds. */
export function multiValueIds(raw: unknown): string[] {
  return Array.isArray(raw)
    ? raw.filter((entry): entry is string => typeof entry === "string")
    : [];
}
