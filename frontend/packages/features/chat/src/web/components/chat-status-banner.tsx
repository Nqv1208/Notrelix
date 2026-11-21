import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";

export type ChatBannerTone = "warning" | "info" | "neutral";

const TONE_CLASS: Record<ChatBannerTone, string> = {
  warning: "bg-warning-subtle text-warning-subtle-foreground",
  info: "bg-info-subtle text-info-subtle-foreground",
  neutral: "bg-muted text-muted-foreground",
};

/**
 * A thin status strip above the message log: the connection is reconnecting,
 * or the channel is archived and read-only. It is a status line for screen
 * readers, so a change in it is announced politely.
 */
export function ChatStatusBanner({
  tone,
  icon,
  children,
  action,
  placement = "bar",
}: {
  /** "notice" is the board's call notice: 20 px in from the pane, 10 above the composer, radius 10, no border. "bar" spans the pane under the header; "composer" is a rounded notice on the conversation column, above the composer. */
  readonly placement?: "bar" | "composer" | "notice";
  readonly tone: ChatBannerTone;
  readonly icon: ReactNode;
  readonly children: ReactNode;
  readonly action?: ReactNode;
}) {
  return (
    <div
      role="status"
      style={
        placement === "notice" ? { fontSize: 13, lineHeight: 1.45 } : undefined
      }
      className={cn(
        "flex shrink-0 flex-wrap items-center",
        placement === "notice"
          ? "mx-5 mb-2.5 gap-2.5 rounded-[10px] px-3 py-2.5 font-semibold"
          : "min-h-9 gap-2 py-1.5 text-sm",
        placement === "bar" && "border-b border-border px-4",
        placement === "composer" &&
          "mx-3 mb-1 rounded-2xl border border-border px-3 md:mx-auto md:w-[calc(100%-2rem)] md:max-w-3xl",
        TONE_CLASS[tone],
      )}
    >
      <span
        className="flex items-center gap-2 [&_svg]:size-4"
        aria-hidden="true"
      >
        {icon}
      </span>
      <span className="min-w-0 flex-1">{children}</span>
      {action ? <span className="ms-auto">{action}</span> : null}
    </div>
  );
}
