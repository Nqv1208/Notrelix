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
      <div className="flex justify-end px-4 pt-2 sm:px-6">
        <span
          role="status"
          className="inline-flex h-7 items-center gap-1.5 rounded-[7px] bg-warning-subtle px-2.5 text-[13px] whitespace-nowrap text-warning-text"
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
    <div className="px-4 pt-3 sm:px-6">
      <div
        role="status"
        className={cn(
          "flex flex-wrap items-start gap-2.5 rounded-[10px] bg-warning-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-warning-text sm:flex-nowrap",
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
