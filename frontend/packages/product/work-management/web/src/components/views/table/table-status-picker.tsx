import { useMemo, useState } from "react";
import { cn } from "@notrelix/ui-web";
import type { MainTableCopy } from "./main-table.copy";
import { StatusDot } from "./table-parts";
import type { TableStatusOption } from "./table-model";

/** Normalised for matching: no case, no diacritics. */
function fold(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .trim();
}

/**
 * The status list of a table cell (listbox): a search field, then one option
 * per Status with its number as a key hint. Arrow keys move the highlight,
 * Enter chooses it, and a digit chooses that option while the field is empty.
 * Escape is handled by the popover that hosts the list.
 */
export function TableStatusPicker({
  copy,
  options,
  currentId,
  initialHighlightId,
  onSelect,
}: {
  copy: MainTableCopy["status"];
  options: readonly TableStatusOption[];
  currentId: string | null;
  /** The option highlighted when the list opens; the current one by default. */
  initialHighlightId?: string | null;
  onSelect: (optionId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [highlightId, setHighlightId] = useState<string | null>(
    initialHighlightId ?? currentId,
  );
  const needle = fold(query);
  const matches = useMemo(
    () =>
      needle ? options.filter((o) => fold(o.name).includes(needle)) : options,
    [needle, options],
  );
  const highlighted =
    matches.find((option) => option.id === highlightId) ?? matches[0] ?? null;

  const move = (step: 1 | -1) => {
    if (matches.length === 0) return;
    const index = highlighted
      ? matches.findIndex((option) => option.id === highlighted.id)
      : -1;
    const next = matches[(index + step + matches.length) % matches.length];
    if (next) setHighlightId(next.id);
  };

  return (
    <>
      <input
        type="text"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder={copy.search}
        aria-label={copy.searchLabel}
        role="combobox"
        aria-expanded="true"
        aria-controls="table-status-listbox"
        aria-activedescendant={
          highlighted ? `table-status-option-${highlighted.id}` : undefined
        }
        onKeyDown={(event) => {
          if (event.key === "ArrowDown") {
            event.preventDefault();
            move(1);
          } else if (event.key === "ArrowUp") {
            event.preventDefault();
            move(-1);
          } else if (event.key === "Enter") {
            event.preventDefault();
            if (highlighted) onSelect(highlighted.id);
          } else if (query === "" && /^[1-9]$/.test(event.key)) {
            const option = options[Number(event.key) - 1];
            if (option) {
              event.preventDefault();
              onSelect(option.id);
            }
          }
        }}
        className="mb-1 box-border h-7.5 w-full rounded-[7px] border border-line-strong bg-background px-[9px] text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
      />
      <div
        role="listbox"
        id="table-status-listbox"
        aria-label={copy.change}
        className="flex flex-col"
      >
        {matches.length === 0 ? (
          <p className="px-2 py-1.5 text-compact text-meta-foreground">
            {copy.none}
          </p>
        ) : (
          matches.map((option) => {
            return (
              <div
                key={option.id}
                id={`table-status-option-${option.id}`}
                role="option"
                aria-selected={option.id === currentId}
                onMouseEnter={() => setHighlightId(option.id)}
                onClick={() => onSelect(option.id)}
                className={cn(
                  "flex h-8 cursor-pointer items-center gap-2 rounded-[7px] px-2 text-compact text-foreground",
                  option.id === highlighted?.id &&
                    "bg-state-hover-strong font-semibold",
                )}
              >
                <StatusDot category={option.category} />
                {option.name}
              </div>
            );
          })
        )}
      </div>
    </>
  );
}
