import { Check, Minus } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { WorkStatusCategory } from "@notrelix/work-management-core";
import { NEUTRAL_DOT_CLASS, STATUS_DOT_CLASS } from "./table-chrome";

/** The 8 px dot before a status name. */
export function StatusDot({
  category,
  className,
}: {
  category: WorkStatusCategory | null;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "box-border inline-block size-2 shrink-0 rounded-full",
        category ? STATUS_DOT_CLASS[category] : NEUTRAL_DOT_CLASS,
        className,
      )}
    />
  );
}

/**
 * The selection checkbox of a row or the header: 15 px, accent when checked,
 * a dash when only some rows are. A native button with the checkbox role so
 * Space and Enter toggle it.
 */
export function TableCheckbox({
  checked,
  label,
  disabled = false,
  onChange,
}: {
  checked: boolean | "mixed";
  label: string;
  disabled?: boolean;
  onChange: (next: boolean) => void;
}) {
  const on = checked !== false;
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={(event) => {
        event.stopPropagation();
        onChange(checked !== true);
      }}
      className={cn(
        "grid size-3.75 shrink-0 place-items-center rounded-[4px] border-[1.5px] p-0 text-transparent",
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
        on
          ? "border-primary bg-primary text-primary-foreground"
          : "border-line-strong bg-popover dark:bg-card",
        disabled ? "cursor-default opacity-60" : "cursor-pointer",
      )}
    >
      {checked === "mixed" ? (
        <Minus className="size-2.75" strokeWidth={3} aria-hidden="true" />
      ) : (
        <Check className="size-2.75" strokeWidth={3} aria-hidden="true" />
      )}
    </button>
  );
}
