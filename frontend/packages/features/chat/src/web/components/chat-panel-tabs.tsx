import { useCallback, useEffect, useRef, useState } from "react";
import { X } from "lucide-react";
import { Button, Tabs, TabsList, TabsTrigger, cn } from "@notrelix/ui-web";
import type { ChatCopy } from "./chat.copy";

export type ChatPanelTabKind = "details" | "pins" | "following" | "mentions";

export const panelTabId = (panelId: string, kind: ChatPanelTabKind) =>
  `${panelId}-tab-${kind}`;

export interface ChatPanelTabsProps {
  /** A thread has no tab: it draws its own header and this strip stays hidden. */
  readonly active: ChatPanelTabKind | "thread";
  /** Which details tab a conversation shows; null in a channel. */
  readonly details: "group" | "direct" | null;
  /** The unread mention count shown as a badge; null hides it. */
  readonly mentionBadge: string | null;
  /** The id of the tabpanel the tabs control. */
  readonly panelId: string;
  readonly hidden: boolean;
  readonly copy: ChatCopy;
  readonly onSelect: (kind: ChatPanelTabKind) => void;
  readonly onClose: () => void;
}

const FADE = "16px";
const trigger =
  "shrink-0 flex-none whitespace-nowrap px-1.5 group-data-[orientation=horizontal]/tabs:after:bottom-0";

/**
 * The side panel's tab strip: short labels that never wrap, each with its full name as a title. When they do not fit the
 * strip scrolls sideways with a fade on the cut edge; the close cross sits outside it, so it is always in view.
 */
export function ChatPanelTabs({
  active,
  details,
  mentionBadge,
  panelId,
  hidden,
  copy,
  onSelect,
  onClose,
}: ChatPanelTabsProps) {
  const listRef = useRef<HTMLDivElement>(null);
  const [edge, setEdge] = useState({ start: false, end: false });

  const measure = useCallback(() => {
    const el = listRef.current;
    if (!el) return;
    const x = Math.abs(el.scrollLeft);
    setEdge({ start: x > 1, end: x < el.scrollWidth - el.clientWidth - 1 });
  }, []);

  useEffect(() => {
    measure();
    const el = listRef.current;
    if (!el || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
  }, [measure, mentionBadge, copy]);

  const fade = `linear-gradient(to right, ${edge.start ? "transparent" : "black"}, black ${edge.start ? FADE : "0px"}, black calc(100% - ${edge.end ? FADE : "0px"}), ${edge.end ? "transparent" : "black"})`;

  const tab = (
    kind: ChatPanelTabKind,
    label: string,
    title: string,
    badge?: string | null,
  ) => (
    <TabsTrigger
      key={kind}
      value={kind}
      id={panelTabId(panelId, kind)}
      aria-controls={panelId}
      title={title}
      className={trigger}
    >
      {label}
      {badge ? (
        <span
          aria-hidden="true"
          className="ms-1.5 rounded-full bg-destructive px-1.5 text-xs font-bold text-destructive-foreground tabular-nums"
        >
          {badge}
        </span>
      ) : null}
    </TabsTrigger>
  );

  return (
    <div
      className={cn(
        "flex h-14 shrink-0 items-center gap-1 border-b border-border ps-2 pe-1.5",
        // The thread and the pins draw their own header and close control at every width (Chat-Thread-*, Chat-Pins-*).
        hidden && "hidden",
      )}
    >
      <Tabs
        value={active}
        // Manual: the arrows move focus, Enter or Space opens; opening Pins or Following would hide this strip under the focus.
        activationMode="manual"
        onValueChange={(value) => onSelect(value as ChatPanelTabKind)}
        className="h-full min-w-0 flex-1 gap-0"
      >
        <TabsList
          ref={listRef}
          variant="line"
          aria-label={copy.panel.label}
          onScroll={measure}
          style={{ maskImage: fade }}
          className="h-full w-full justify-start gap-0 overflow-x-auto overflow-y-hidden p-0 [scrollbar-width:none] group-data-horizontal/tabs:h-full [&::-webkit-scrollbar]:hidden"
        >
          {details
            ? tab(
                "details",
                details === "group"
                  ? copy.panel.tabGroup
                  : copy.dm.details.dmTitle,
                details === "group"
                  ? copy.dm.details.groupTitle
                  : copy.dm.details.dmTitle,
              )
            : null}
          {tab("pins", copy.pins.tab, copy.pins.title)}
          {tab("following", copy.panel.tabFollowing, copy.thread.followedTab)}
          {tab(
            "mentions",
            copy.mentions.tab,
            copy.mentions.panelTitle,
            mentionBadge,
          )}
        </TabsList>
      </Tabs>
      <Button
        type="button"
        size="icon-sm"
        variant="ghost"
        className="shrink-0"
        aria-label={copy.pins.close}
        title={copy.pins.close}
        onClick={onClose}
      >
        <X aria-hidden="true" />
      </Button>
    </div>
  );
}
