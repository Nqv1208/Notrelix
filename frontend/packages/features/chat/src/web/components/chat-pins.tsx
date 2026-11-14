import { Pin, PinOff } from "lucide-react";
import { Avatar, AvatarFallback, Button, Skeleton, cn } from "@notrelix/ui-web";
import type { ChatMessage, ChatPin } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { formatClock } from "./chat-message-row";
import { ChatPanelHeader } from "./chat-panel-header";
import { fillCopy, pluralCopy } from "./use-chat-copy";

export interface PinnedBarProps {
  readonly copy: ChatCopy;
  readonly count: number;
  /** The newest pin, shown as the bar's preview. */
  readonly newest: ChatMessage | null;
  readonly newestAuthor: string;
  readonly onOpen: () => void;
}

/**
 * The pinned bar under the channel header: the newest pin and the count. The
 * whole bar opens the Pins panel; it is absent when nothing is pinned.
 */
export function PinnedBar({
  copy,
  count,
  newest,
  newestAuthor,
  onOpen,
}: PinnedBarProps) {
  if (count === 0 || newest === null) return null;
  const preview = newest.deleted ? copy.list.deleted : (newest.content ?? "");
  return (
    <div
      className="flex items-center gap-2 border-b border-border bg-muted/40 px-4 py-1.5 text-sm"
      data-testid="chat-pinned-bar"
    >
      <Pin
        className="size-3.5 shrink-0 text-muted-foreground"
        aria-hidden="true"
      />
      <span className="shrink-0 text-xs font-semibold text-muted-foreground">
        {copy.pins.bar}
      </span>
      <button
        type="button"
        onClick={onOpen}
        aria-label={`${copy.pins.open}: ${newestAuthor}, ${preview}`}
        className="flex min-w-0 flex-1 items-center gap-2 rounded-md px-1 py-0.5 text-start hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
      >
        <span className="min-w-0 truncate">
          <span className="font-semibold">{newestAuthor}: </span>
          {preview}
        </span>
        <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
          {pluralCopy(
            { one: copy.pins.barCount_one, other: copy.pins.barCount_other },
            count,
            { count },
          )}
        </span>
      </button>
    </div>
  );
}

export interface PinsPanelProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly channelId: string;
  readonly copy: ChatCopy;
  readonly locale: string;
  readonly timeZone?: string;
  readonly authorName: (userId: string) => string;
  /** The channel as the header names it, such as "# erp-rollout". */
  readonly spaceLabel: string;
  readonly onClose: () => void;
  readonly canUnpin: (pin: ChatPin) => boolean;
  readonly onUnpin: (message: ChatMessage) => void;
  readonly unpinBusy: boolean;
}

/** The Pins panel: the channel's pinned messages, newest pin first, a page at a time. */
export function PinsPanel({
  hooks,
  workspaceId,
  channelId,
  copy,
  locale,
  timeZone,
  authorName,
  spaceLabel,
  onClose,
  canUnpin,
  onUnpin,
  unpinBusy,
}: PinsPanelProps) {
  const pins = hooks.usePins(workspaceId, channelId);
  const items = pins.data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <section
      aria-label={copy.pins.title}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-pins-panel"
    >
      <ChatPanelHeader
        title={copy.pins.title}
        subtitle={fillCopy(copy.pins.subtitle, {
          space: spaceLabel,
          count: String(items.length),
        })}
        closeLabel={copy.pins.close}
        onClose={onClose}
      />
      {items.length > 0 ? (
        <p className="border-b border-border px-4 py-2.5 text-[13px] text-muted-foreground max-md:hidden">
          {fillCopy(copy.pins.summary, { count: String(items.length) })}
        </p>
      ) : null}
      {pins.isPending ? (
        <div role="status" className="flex flex-col gap-3 p-4">
          <span className="sr-only">{copy.pins.title}</span>
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      ) : pins.isError ? (
        <div className="flex flex-col items-start gap-2 p-4">
          <p role="alert" className="text-sm text-muted-foreground">
            {copy.pins.error}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => void pins.refetch()}
          >
            {copy.list.retry}
          </Button>
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-start gap-1 p-4">
          <p className="text-sm font-medium">{copy.pins.empty}</p>
          <p className="text-sm text-muted-foreground">
            {copy.pins.empty_body}
          </p>
        </div>
      ) : (
        <ul
          className="flex min-h-0 flex-1 flex-col overflow-y-auto"
          aria-label={copy.pins.title}
        >
          {items.map((pin) => {
            const author = authorName(pin.message.authorId);
            return (
              <li
                key={pin.message.id}
                className="border-b border-border px-4 py-3"
                data-testid="chat-pin-item"
              >
                <div className="flex items-center gap-2">
                  <Avatar className="size-[18px]" aria-hidden="true">
                    <AvatarFallback
                      className={cn(
                        "text-[9px] font-[650]",
                        avatarToneOf(pin.message.authorId),
                      )}
                    >
                      {initialsOf(author)}
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-[13px] font-bold">{author}</span>
                  <time
                    dateTime={pin.message.createdAt}
                    className="text-[11.5px] font-medium tracking-[.01em] text-muted-foreground tabular-nums"
                  >
                    {pinDayLabel(pin.message.createdAt, locale, timeZone)}
                  </time>
                  <span className="flex-1" />
                  {canUnpin(pin) ? (
                    <Button
                      type="button"
                      size="icon-sm"
                      variant="ghost"
                      className="size-7 text-muted-foreground"
                      disabled={unpinBusy}
                      title={copy.toolbar.unpin}
                      aria-label={`${copy.toolbar.unpin}: ${pin.message.content ?? ""}`}
                      onClick={() => onUnpin(pin.message)}
                    >
                      <PinOff aria-hidden="true" />
                    </Button>
                  ) : null}
                </div>
                <p className="mt-1.5 mb-1 line-clamp-3 text-sm leading-[1.5] break-words whitespace-pre-wrap">
                  {pin.message.deleted
                    ? copy.list.deleted
                    : pin.message.content}
                </p>
                <p className="text-xs text-muted-foreground">
                  {fillCopy(copy.pins.pinnedBy, {
                    name: authorName(pin.pinnedBy),
                  })}
                  <span className="max-md:hidden">
                    {" · "}
                    {formatClock(pin.pinnedAt, locale, timeZone)}
                  </span>
                </p>
              </li>
            );
          })}
        </ul>
      )}
      {pins.hasNextPage ? (
        <div className="flex justify-center p-2">
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={pins.isFetchingNextPage}
            onClick={() => void pins.fetchNextPage()}
          >
            {copy.pins.loadMore}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

/** "Thứ Hai 10/11": the weekday and the day and month, as the Pins board draws a pinned message's date. */
function pinDayLabel(iso: string, locale: string, timeZone?: string): string {
  const parts = new Intl.DateTimeFormat(locale, {
    weekday: "long",
    day: "2-digit",
    month: "2-digit",
    ...(timeZone ? { timeZone } : {}),
  }).formatToParts(new Date(iso));
  const part = (type: string) =>
    parts.find((entry) => entry.type === type)?.value ?? "";
  return `${part("weekday")} ${part("day")}/${part("month")}`;
}
