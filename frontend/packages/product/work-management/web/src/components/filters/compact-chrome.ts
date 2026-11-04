/** The phone bar (under 640 px) as `WM-Filter-320` draws it: 36 px pills and icon buttons. Literal strings so Tailwind generates them. */
export const compactPillClass =
  "h-9 gap-1.5 rounded-full border border-line-strong bg-popover px-2.5 text-[12.5px] font-medium text-muted-foreground hover:bg-state-hover-strong hover:text-foreground";

export const compactPillOnClass =
  "border-brand/40 bg-state-selected text-state-selected-foreground hover:bg-state-selected";

export const compactIconButtonClass =
  "grid size-9 flex-none cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent p-0 text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring";

/** A native select in a sheet: the artboard's `.inp.sel`, 34 px high. */
export const compactSelectClass =
  "h-8.5 w-full min-w-0 rounded-[7px] border border-line-strong bg-popover px-2.5 text-[13.5px] text-foreground focus-visible:outline-2 focus-visible:outline-ring";

/** The two sheets: up to 94 % of the screen, the header and footer fixed, the body scrolling. */
export const sheetClass =
  "max-h-[94dvh] gap-0 rounded-t-[16px] border-line p-0 data-[side=bottom]:h-auto";
