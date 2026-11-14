import { LoaderCircle, TriangleAlert } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillBoardHeaderCopy } from "./board-header.copy";
import type { BoardHeaderCopy } from "./board-header.copy";

export type BoardConnectionState =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "offline"
  | "closed"
  | "failed";

/** A toast over the board's bottom edge: it never takes room from the content, so nothing shifts when it comes or goes. */
const TOAST_BOX =
  "pointer-events-none fixed inset-x-0 bottom-[max(1rem,env(safe-area-inset-bottom))] z-50 flex justify-center px-4";

interface BoardConnectionNoticeProps {
  state: BoardConnectionState;
  /** When the connection was lost; names the time in the notice. */
  lostAt: Date | null;
  copy: BoardHeaderCopy;
  locale: string;
  onReconnect: () => void;
}

/**
 * The live connection of the open board. Quiet while connected (nothing shows);
 * a pill while it reconnects; a notice with Reconnect once it is lost, which says
 * the data may be old and that a write fails with Try again (BC-Realtime).
 */
export function BoardConnectionNotice({
  state,
  lostAt,
  copy,
  locale,
  onReconnect,
}: BoardConnectionNoticeProps) {
  const t = copy.connection;
  if (state === "reconnecting")
    return (
      <div className={TOAST_BOX}>
        <span
          role="status"
          className="pointer-events-auto inline-flex h-9 items-center gap-2 rounded-[10px] border border-line-strong bg-card px-3.5 text-[13px] whitespace-nowrap text-warning-text shadow-popover animate-in fade-in-0 slide-in-from-bottom-2 duration-200 motion-reduce:animate-none"
        >
          <LoaderCircle
            aria-hidden="true"
            className="size-4 animate-spin motion-reduce:animate-none"
          />
          {t.reconnecting}
        </span>
      </div>
    );
  if (state !== "offline" && state !== "failed" && state !== "closed")
    return null;
  const time = (lostAt ?? new Date()).toLocaleTimeString(locale, {
    hour: "2-digit",
    minute: "2-digit",
  });
  return (
    <div className={TOAST_BOX}>
      <div
        role="status"
        className={cn(
          "pointer-events-auto flex w-full max-w-xl flex-wrap items-start gap-2.5 rounded-[12px] border border-line-strong bg-card px-3.5 py-2.5 text-[13px] leading-[1.4] text-warning-text shadow-popover animate-in fade-in-0 slide-in-from-bottom-2 duration-200 motion-reduce:animate-none sm:flex-nowrap",
        )}
      >
        <TriangleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
        <span className="flex min-w-0 flex-1 flex-col gap-px max-sm:basis-[calc(100%-26px)]">
          <b className="font-semibold text-foreground">
            {fillBoardHeaderCopy(t.lostTitle, { time })}
          </b>
          <span>{t.lostBody}</span>
        </span>
        <button
          type="button"
          onClick={onReconnect}
          className="inline-flex h-11 flex-1 cursor-pointer items-center justify-center rounded-[7px] border border-line-strong bg-card px-3.5 text-[13px] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring max-sm:basis-full sm:flex-none"
        >
          {t.reconnect}
        </button>
      </div>
    </div>
  );
}
