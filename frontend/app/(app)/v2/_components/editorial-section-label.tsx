import { cn } from "@/lib/utils"

type SectionLabelProps = {
  /** e.g. "02" */
  index: string
  /** e.g. "08" — total sections */
  total?: string
  label: string
  className?: string
}

/**
 * The mono caption row that opens each section:
 *   [ 02 / 08 ] ── BOARDS
 * A thin accent tick + hairline rule give the Swiss-editorial cadence.
 */
export function SectionLabel({
  index,
  total = "08",
  label,
  className,
}: SectionLabelProps) {
  return (
    <div className={cn("flex items-center gap-4", className)}>
      <span className="ed-mono ed-ink-faint text-xs tabular-nums tracking-widest">
        {index}
        <span className="px-1 opacity-50">/</span>
        {total}
      </span>
      <span className="ed-accent text-base leading-none">—</span>
      <span className="ed-eyebrow">{label}</span>
      <span aria-hidden className="ed-rule ml-2 hidden h-px flex-1 border-t sm:block" />
    </div>
  )
}
