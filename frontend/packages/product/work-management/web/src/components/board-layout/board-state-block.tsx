import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";

/**
 * The centred note of a board state (BC-Shell Er, E, Ev, NP): a 44 px icon tile, a title, one line of
 * explanation and at most one action. It sits on the page, not in a card.
 */
export function BoardStateBlock({
  icon,
  title,
  description,
  action,
  role = "status",
  className,
}: {
  icon: ReactNode;
  title: string;
  description: string;
  action?: ReactNode;
  role?: "status" | "alert";
  className?: string;
}) {
  return (
    <div
      role={role}
      data-slot="board-state-block"
      className={cn(
        "box-border flex flex-col items-center gap-1.5 px-6 py-10 text-center",
        className,
      )}
    >
      <span
        aria-hidden="true"
        className="grid size-11 place-items-center rounded-[12px] bg-state-hover-strong text-meta-foreground [&_svg]:size-5"
      >
        {icon}
      </span>
      <b className="text-[16px] leading-[1.45] font-[650] text-foreground">
        {title}
      </b>
      <span className="max-w-[46ch] text-[13.5px] leading-[1.45] text-muted-foreground">
        {description}
      </span>
      {action ? <div className="mt-1.5">{action}</div> : null}
    </div>
  );
}

/** A quiet info or warning band under the tabs (BC-Shell VM, NG). */
export function BoardBand({
  tone,
  icon,
  title,
  description,
  action,
}: {
  tone: "info" | "warning";
  icon: ReactNode;
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div
      role="status"
      data-slot="board-band"
      className={
        tone === "info"
          ? "flex flex-wrap items-start gap-2.5 rounded-[10px] bg-info-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-info-text sm:flex-nowrap"
          : "flex flex-wrap items-start gap-2.5 rounded-[10px] bg-warning-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-warning-text sm:flex-nowrap"
      }
    >
      <span aria-hidden="true" className="mt-0.5 shrink-0 [&_svg]:size-4">
        {icon}
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-px max-sm:basis-[calc(100%-26px)]">
        <b className="font-semibold text-foreground">{title}</b>
        <span>{description}</span>
      </span>
      {action ? (
        <span className="flex shrink-0 items-center max-sm:basis-full max-sm:[&>*]:h-11 max-sm:[&>*]:flex-1">
          {action}
        </span>
      ) : null}
    </div>
  );
}

/** The 32 px outline button of a band or a state block. */
export const BOARD_BAND_BUTTON =
  "inline-flex h-8 max-sm:h-11 max-sm:px-3.5 max-sm:text-[14px] cursor-pointer items-center justify-center gap-1.5 rounded-[7px] border border-line-strong [&_svg]:size-4 bg-popover px-3 text-[13px] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring dark:bg-card";
