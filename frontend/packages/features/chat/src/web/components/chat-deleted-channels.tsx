import { useState } from "react";
import { Hash, Lock, Trash2 } from "lucide-react";
import {
  Button,
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Skeleton,
} from "@notrelix/ui-web";
import { toast } from "sonner";
import { chatFailureOf } from "../../core/api/chat-failure";
import type { ChatDeletedChannel } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { fillCopy, pluralCopy } from "./use-chat-copy";

export interface ChatDeletedChannelsProps {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly copy: ChatCopy;
  /** The name of the person who deleted a channel; the list carries only their id. */
  readonly nameOf: (userId: string) => string;
  readonly locale?: string;
  readonly timeZone?: string;
  /** Called with the restored channel once the server accepted it. */
  readonly onRestored?: (channel: ChatDeletedChannel) => void;
  /** The small-screen back button; absent on desktop, where the sidebar stays beside the list. */
  readonly onClose?: () => void;
}

/** "3 days ago" in the viewer's language: whole days from a day on, whole hours or minutes before. */
export function deletedAgo(
  deletedAt: string,
  now: number,
  locale: string,
): string {
  const format = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  const minutes = Math.max(
    0,
    Math.floor((now - Date.parse(deletedAt)) / 60000),
  );
  if (minutes < 60) return format.format(-minutes, "minute");
  if (minutes < 1440) return format.format(-Math.floor(minutes / 60), "hour");
  return format.format(-Math.floor(minutes / 1440), "day");
}

/**
 * The deleted channels the caller may restore (UC-CHT-A08), newest delete first, each with the last day it can be
 * restored. Chat-Var-Deleted draws the list; -Loading, -Empty and -Error draw the other states and -Conflict the
 * name-taken answer, kept on the row it belongs to. One restore runs at a time per row; the row leaves the list when
 * the server accepts, and the channel comes back to the sidebar through the channel refresh.
 */
export function ChatDeletedChannels({
  hooks,
  workspaceId,
  copy,
  nameOf,
  locale = "vi",
  timeZone,
  onRestored,
  onClose,
}: ChatDeletedChannelsProps) {
  const text = copy.deleted;
  const list = hooks.useDeletedChannels(workspaceId, true);
  const restorer = hooks.useRestoreChannel(workspaceId);
  const [pendingId, setPendingId] = useState<string | null>(null);
  // The answer to the last restore of one row; a new attempt clears it.
  const [problem, setProblem] = useState<{
    readonly id: string;
    readonly message: string;
  } | null>(null);

  const rows = list.data ?? [];
  const now = Date.now();
  const date = new Intl.DateTimeFormat(locale, {
    dateStyle: "short",
    timeZone,
  });

  const restore = (row: ChatDeletedChannel) => {
    setProblem(null);
    setPendingId(row.id);
    restorer.mutate(
      { channelId: row.id, expectedVersion: row.version },
      {
        onSuccess: () => {
          toast.success(fillCopy(text.restored, { name: row.name }), {
            id: `chat-restored-${row.id}`,
          });
          onRestored?.(row);
        },
        onError: (error) => {
          const failure = chatFailureOf(error);
          const message =
            failure.kind === "name-conflict"
              ? fillCopy(text.nameTaken, { name: row.name })
              : failure.kind === "restore-window-elapsed"
                ? text.windowElapsed
                : failure.kind === "not-found"
                  ? text.gone
                  : failure.kind === "stale"
                    ? text.stale
                    : text.failed;
          setProblem({ id: row.id, message });
        },
        onSettled: () => setPendingId(null),
      },
    );
  };

  return (
    <div
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-deleted-channels"
    >
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-border px-4">
        {onClose ? (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="md:hidden"
            onClick={onClose}
          >
            {copy.sidebar.closeList}
          </Button>
        ) : null}
        <h2 className="text-base font-semibold">{text.title}</h2>
        <span className="text-sm text-muted-foreground">
          {list.isPending
            ? text.loading
            : list.isError || rows.length === 0
              ? null
              : pluralCopy(
                  { one: text.count_one, other: text.count_other },
                  rows.length,
                  { count: rows.length },
                )}
        </span>
      </header>

      <div className="flex min-h-0 flex-1 flex-col overflow-auto p-4 md:px-8 md:py-6">
        {list.isPending ? (
          <div
            role="status"
            aria-busy="true"
            aria-label={text.loading}
            className="overflow-hidden rounded-xl border border-border"
          >
            {[0, 1, 2].map((index) => (
              <div
                key={index}
                className="flex items-center gap-3.5 border-t border-border p-[18px] first:border-t-0"
              >
                <Skeleton className="size-[18px] rounded" />
                <div className="flex flex-1 flex-col gap-2">
                  <Skeleton className="h-3 w-44" />
                  <Skeleton className="h-2.5 w-64 max-w-full" />
                </div>
                <Skeleton className="h-[30px] w-[84px] rounded-lg" />
              </div>
            ))}
          </div>
        ) : list.isError ? (
          <div
            role="alert"
            className="flex flex-col items-center gap-3 py-24 text-center"
          >
            <p className="text-base font-semibold">{text.error}</p>
            <Button variant="outline" onClick={() => void list.refetch()}>
              {text.retry}
            </Button>
          </div>
        ) : rows.length === 0 ? (
          <Empty className="flex-1">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <Trash2 aria-hidden="true" />
              </EmptyMedia>
              <EmptyTitle>{text.emptyTitle}</EmptyTitle>
              <EmptyDescription>{text.emptyBody}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <ul className="overflow-hidden rounded-xl border border-border">
            <li
              aria-hidden="true"
              className="flex px-[18px] py-2.5 text-xs font-semibold text-muted-foreground max-md:hidden"
            >
              <span className="flex-1">{text.columnChannel}</span>
              <span>{text.columnDeadline}</span>
            </li>
            {rows.map((row) => {
              const Icon = row.kind === "channel_private" ? Lock : Hash;
              const busy = pendingId === row.id;
              const answer = problem?.id === row.id ? problem.message : null;
              return (
                <li
                  key={row.id}
                  className="flex items-start gap-3.5 border-t border-border px-[18px] py-3.5 max-md:flex-col"
                >
                  <div className="flex min-w-0 flex-1 items-start gap-3.5">
                    <Icon
                      className="mt-0.5 size-[18px] shrink-0 text-muted-foreground"
                      aria-hidden="true"
                    />
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-semibold">
                        {row.name}
                      </div>
                      <div className="mt-0.5 text-xs text-muted-foreground">
                        {row.deletedBy
                          ? fillCopy(text.deletedBy, {
                              name: nameOf(row.deletedBy),
                            })
                          : text.deletedByUnknown}
                        {" · "}
                        {deletedAgo(row.deletedAt, now, locale)}
                      </div>
                      {answer ? (
                        <p
                          role="alert"
                          className="mt-1.5 text-xs text-destructive"
                        >
                          {answer}
                        </p>
                      ) : null}
                    </div>
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1.5 max-md:w-full max-md:items-stretch">
                    <span className="text-xs text-muted-foreground">
                      {fillCopy(text.until, {
                        date: date.format(new Date(row.restorableUntil)),
                      })}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      className="max-md:min-h-11"
                      disabled={busy}
                      aria-label={fillCopy(text.restoreLabel, {
                        name: row.name,
                      })}
                      onClick={() => restore(row)}
                    >
                      {busy ? text.restoring : text.restore}
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}
