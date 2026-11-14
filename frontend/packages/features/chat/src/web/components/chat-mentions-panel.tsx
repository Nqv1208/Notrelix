import { AtSign } from "lucide-react";
import type { ChatUnreadRow } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { pluralCopy } from "./use-chat-copy";

export interface MentionsPanelProps {
  /** The caller's unread rows; the panel lists the ones with unread mentions that are not muted. */
  readonly rows: readonly ChatUnreadRow[];
  readonly copy: ChatCopy;
  readonly nameOf: (row: ChatUnreadRow) => string;
  readonly onOpen: (spaceId: string) => void;
}

/**
 * Where the caller is mentioned and has not read yet (the "Nhắc đến" tab of the design), from the unread summary the sidebar
 * already holds: one row per space with its count, most mentions first. Opening a row opens the space.
 */
export function MentionsPanel({
  rows,
  copy,
  nameOf,
  onOpen,
}: MentionsPanelProps) {
  const listed = rows
    .filter((row) => row.mentionCount > 0 && !row.muted)
    .sort(
      (a, b) =>
        b.mentionCount - a.mentionCount || a.spaceId.localeCompare(b.spaceId),
    );
  return (
    <section
      aria-label={copy.mentions.panelTitle}
      className="flex min-h-0 flex-1 flex-col"
      data-testid="chat-mentions-panel"
    >
      <h2 className="border-b border-border px-4 py-2.5 text-sm font-semibold">
        {copy.mentions.panelTitle}
      </h2>
      {listed.length === 0 ? (
        <p className="p-4 text-sm text-muted-foreground">
          {copy.mentions.panelEmpty}
        </p>
      ) : (
        <ul className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto p-2">
          {listed.map((row) => (
            <li key={row.spaceId}>
              <button
                type="button"
                onClick={() => onOpen(row.spaceId)}
                className="flex w-full items-center gap-2 rounded-md border border-border p-2.5 text-start text-sm hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
              >
                <AtSign
                  className="size-4 shrink-0 text-destructive"
                  aria-hidden="true"
                />
                <span className="min-w-0 flex-1 truncate font-medium">
                  {nameOf(row)}
                </span>
                <span className="shrink-0 text-xs text-muted-foreground tabular-nums">
                  {pluralCopy(
                    {
                      one: copy.mentions.panelRow_one,
                      other: copy.mentions.panelRow_other,
                    },
                    row.mentionCount,
                  )}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
