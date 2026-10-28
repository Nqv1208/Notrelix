import { ChevronDown, ChevronsUp, Equal, Flag } from "lucide-react";
import { cn } from "@notrelix/ui-web";

export type PriorityLevel = "urgent" | "high" | "medium" | "low";

/** Text tone of each level; the glyph differs too, so colour is never the only signal. */
export const PRIORITY_TONE_CLASS: Readonly<Record<PriorityLevel, string>> = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-muted-foreground",
  low: "text-meta-foreground",
};

/**
 * The glyph of a priority level, the same on the Kanban card, Calendar,
 * Timeline and Table: filled flag (urgent), double chevron up (high), two
 * bars (medium), chevron down (low). Decorative: callers name the priority.
 */
export function PriorityGlyph({
  level,
  className,
}: {
  level: PriorityLevel;
  className?: string;
}) {
  const props = {
    "aria-hidden": true,
    className: cn("size-3.25 shrink-0", className),
  } as const;
  if (level === "urgent") return <Flag {...props} fill="currentColor" />;
  if (level === "high") return <ChevronsUp {...props} />;
  if (level === "medium") return <Equal {...props} />;
  return <ChevronDown {...props} />;
}
