import { Pin, PinOff } from "lucide-react";
import { Button, Skeleton } from "@notrelix/ui-web";
import type { ChatMessage, ChatPin } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { formatClock } from "./chat-message-row";
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
      <h2 className="border-b border-border px-4 py-2.5 text-sm font-semibold">
        {copy.pins.title}
      </h2>
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
          className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto p-2"
          aria-label={copy.pins.title}
        >
          {items.map((pin) => (
            <li
              key={pin.message.id}
              className="flex flex-col gap-1 rounded-md border border-border p-2.5"
              data-testid="chat-pin-item"
            >
              <div className="flex items-baseline gap-2 text-xs text-muted-foreground">
                <span className="font-semibold text-foreground">
                  {authorName(pin.message.authorId)}
                </span>
                <time dateTime={pin.message.createdAt} className="tabular-nums">
                  {formatClock(pin.message.createdAt, locale, timeZone)}
                </time>
              </div>
              <p className="line-clamp-3 whitespace-pre-wrap break-words text-sm">
                {pin.message.deleted ? copy.list.deleted : pin.message.content}
              </p>
              <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
                <span>
                  {fillCopy(copy.pins.pinnedBy, {
                    name: authorName(pin.pinnedBy),
                  })}
                </span>
                {canUnpin(pin) ? (
                  <Button
                    type="button"
                    size="xs"
                    variant="ghost"
                    disabled={unpinBusy}
                    aria-label={`${copy.toolbar.unpin}: ${pin.message.content ?? ""}`}
                    onClick={() => onUnpin(pin.message)}
                  >
                    <PinOff aria-hidden="true" />
                    {copy.toolbar.unpin}
                  </Button>
                ) : null}
              </div>
            </li>
          ))}
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
