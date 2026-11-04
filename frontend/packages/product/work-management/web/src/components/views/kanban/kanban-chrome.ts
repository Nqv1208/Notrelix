/**
 * Class strings for the Kanban chrome that the shared `Button` does not give
 * at the mockup's size: the flat 32 px toolbar pill (Lọc, Nhóm theo), the
 * 32 px outline button and the primary action. Literal strings so Tailwind
 * generates them.
 */
export const kanbanPillClass =
  "h-8 gap-1.5 rounded-[7px] border-0 px-2.5 text-compact font-normal text-muted-foreground hover:bg-state-hover-strong hover:text-foreground aria-expanded:bg-state-hover-strong aria-expanded:text-foreground tablet:h-11 tablet:gap-[7px] tablet:rounded-[10px] tablet:border tablet:border-line-strong tablet:bg-popover tablet:px-3.5 tablet:text-[14px] tablet:font-medium tablet:[&_svg]:size-3.75";

export const kanbanOutlineButtonClass =
  "h-8 gap-1.5 rounded-[7px] border-line-strong bg-popover px-3 text-compact font-medium text-foreground shadow-none hover:bg-state-hover-strong";

export const kanbanPrimaryButtonClass =
  "h-8 gap-1.5 rounded-[7px] px-3 text-compact font-semibold";

/**
 * Whether the user asked for reduced motion. Read when a card renders, so a
 * drag that starts after the setting changed already follows it.
 */
export function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}
