import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";

export type ChatBannerTone = "warning" | "info";

const TONE_CLASS: Record<ChatBannerTone, string> = {
  warning: "bg-warning-subtle text-warning-subtle-foreground",
  info: "bg-info-subtle text-info-subtle-foreground",
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
  /** "bar" spans the pane under the header; "composer" is a rounded notice on the conversation column, above the composer. */
  readonly placement?: "bar" | "composer";
  readonly tone: ChatBannerTone;
  readonly icon: ReactNode;
  readonly children: ReactNode;
  readonly action?: ReactNode;
}) {
  return (
    <div
      role="status"
      className={cn(
        "flex min-h-9 shrink-0 flex-wrap items-center gap-2 py-1.5 text-sm",
        placement === "bar"
          ? "border-b border-border px-4"
          : "mx-3 mb-1 rounded-2xl border border-border px-3 md:mx-auto md:w-[calc(100%-2rem)] md:max-w-3xl",
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
