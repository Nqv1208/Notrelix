import { useState } from "react";
import { Search, X } from "lucide-react";
import { compactIconButtonClass } from "./compact-chrome";
import { useFiltersCopy } from "./use-filters-copy";

export interface CompactSearchProps {
  value: string;
  onChange: (value: string) => void;
  /** The input's label and the icon button's name. */
  label: string;
  placeholder: string;
}

/**
 * The phone toolbar's search: an icon button that turns into the input on its
 * own line, so the bar keeps Lọc, Cách xem and the saved filters on one row.
 * It stays open while there is a query.
 */
export function CompactSearch({
  value,
  onChange,
  label,
  placeholder,
}: CompactSearchProps) {
  const closeLabel = useFiltersCopy().sheet.close;
  const [open, setOpen] = useState(value !== "");
  if (!open && value === "")
    return (
      <button
        type="button"
        aria-label={label}
        aria-expanded={false}
        onClick={() => setOpen(true)}
        className={compactIconButtonClass}
      >
        <Search className="size-4" aria-hidden="true" />
      </button>
    );
  return (
    <label className="flex h-9 basis-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover pr-1 pl-2.5 text-meta-foreground focus-within:ring-2 focus-within:ring-focus-ring dark:bg-card">
      <Search className="size-4 shrink-0" aria-hidden="true" />
      <input
        type="search"
        // Opened by the icon: the keyboard goes straight to the field.
        autoFocus
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        aria-label={label}
        className="min-w-0 flex-1 bg-transparent text-compact text-foreground outline-none placeholder:text-meta-foreground [&::-webkit-search-cancel-button]:hidden"
      />
      <button
        type="button"
        aria-label={closeLabel}
        onClick={() => {
          onChange("");
          setOpen(false);
        }}
        className="grid size-7 cursor-pointer place-items-center rounded-md border-0 bg-transparent p-0 text-inherit hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
      >
        <X className="size-4" aria-hidden="true" />
      </button>
    </label>
  );
}
