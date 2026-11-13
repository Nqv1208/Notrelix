import { RotateCw, SquarePen, Users } from "lucide-react";
import { Avatar, AvatarFallback, Button, Skeleton, cn } from "@notrelix/ui-web";
import {
  conversationTitle,
  directCounterpart,
} from "../../core/model/conversation-model";
import type { ChatConversation } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { pluralCopy } from "./use-chat-copy";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";

export type ConversationListStatus = "loading" | "ready" | "error";

/** What the sidebar needs to list the caller's conversations. Hidden ones are left out by the list itself. */
export interface ChatConversationListView {
  readonly conversations: readonly ChatConversation[];
  readonly status: ConversationListStatus;
  readonly activeId: string | null;
  readonly viewerId: string;
  readonly locale: string;
  readonly timeZone?: string;
  readonly nameOf: (userId: string) => string;
  /** The unread count of a conversation (the unread summary is fresher than the list). */
  readonly unreadOf: (conversation: ChatConversation) => number;
  /** The presence of a person (the dot of a direct conversation). Without it no dot is shown. */
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly onSelect: (conversationId: string) => void;
  /** Absent for a Guest: a Guest starts no conversation (UC-CHT-H02). */
  readonly onNewMessage?: () => void;
  readonly onRetry: () => void;
}

const HEADING =
  "px-2 pt-4 pb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground";

/** The short time of the newest activity: the clock today, the day otherwise. */
export function activityLabel(
  iso: string,
  now: Date,
  locale: string,
  timeZone?: string,
): string {
  const at = new Date(iso);
  if (Number.isNaN(at.getTime())) return "";
  const sameDay =
    at.toLocaleDateString(locale, { timeZone }) ===
    now.toLocaleDateString(locale, { timeZone });
  return sameDay
    ? at.toLocaleTimeString(locale, {
        hour: "2-digit",
        minute: "2-digit",
        timeZone,
      })
    : at.toLocaleDateString(locale, {
        day: "numeric",
        month: "numeric",
        timeZone,
      });
}

/** The preview line: "You: text" for the caller's own message, "Name: text" in a group, the text in a direct one. */
export function previewOf(
  conversation: ChatConversation,
  viewerId: string,
  nameOf: (userId: string) => string,
  copy: ChatCopy,
): string {
  const last = conversation.lastMessage;
  if (!last || last.text === null || last.text === "") {
    return last ? "" : copy.dm.sidebar.noMessages;
  }
  if (last.kind === "system") return last.text;
  if (last.authorId === viewerId)
    return `${copy.dm.sidebar.youPrefix}${last.text}`;
  return conversation.kind === "group"
    ? `${nameOf(last.authorId)}: ${last.text}`
    : last.text;
}

/** The "Tin nhắn trực tiếp" section of the sidebar: one row per conversation, newest activity first. */
export function ChatConversationSection({
  view,
  copy,
}: {
  readonly view: ChatConversationListView;
  readonly copy: ChatCopy;
}) {
  const now = new Date();
  return (
    <section
      aria-labelledby="chat-dm-heading"
      className="flex flex-col"
      data-testid="chat-dm-section"
    >
      <div className="flex items-center justify-between ps-2 pe-1">
        <h3 id="chat-dm-heading" className={HEADING}>
          {copy.dm.sidebar.heading}
        </h3>
        {view.onNewMessage ? (
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={copy.dm.sidebar.newMessage}
            title={copy.dm.sidebar.newMessage}
            onClick={view.onNewMessage}
          >
            <SquarePen aria-hidden="true" />
          </Button>
        ) : null}
      </div>
      {view.status === "loading" ? (
        <div role="status" className="space-y-2 px-1 pt-1">
          <span className="sr-only">{copy.dm.sidebar.loading}</span>
          {[0, 1].map((index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      ) : view.status === "error" ? (
        <div className="space-y-2 px-1 pt-1 text-sm">
          <p className="text-muted-foreground">{copy.dm.sidebar.error}</p>
          <Button variant="outline" size="sm" onClick={view.onRetry}>
            <RotateCw aria-hidden="true" />
            {copy.dm.sidebar.retry}
          </Button>
        </div>
      ) : view.conversations.length === 0 ? (
        <p className="px-2 text-sm text-muted-foreground">
          {copy.dm.sidebar.empty}
        </p>
      ) : (
        <ul className="flex flex-col gap-0.5">
          {view.conversations.map((conversation) => (
            <li key={conversation.id}>
              <ConversationRow
                conversation={conversation}
                view={view}
                copy={copy}
                now={now}
              />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function ConversationRow({
  conversation,
  view,
  copy,
  now,
}: {
  readonly conversation: ChatConversation;
  readonly view: ChatConversationListView;
  readonly copy: ChatCopy;
  readonly now: Date;
}) {
  const title = conversationTitle(conversation, view.viewerId, view.nameOf);
  const preview = previewOf(conversation, view.viewerId, view.nameOf, copy);
  const active = conversation.id === view.activeId;
  const count = view.unreadOf(conversation);
  const muted = conversation.notificationLevel === "none";
  const unreadHere = count > 0 && !muted;
  const badge = unreadHere ? (count > 99 ? "99+" : String(count)) : null;
  const counterpart = directCounterpart(conversation, view.viewerId);
  const presence =
    counterpart && conversation.kind === "direct"
      ? view.presenceOf?.(counterpart)
      : undefined;
  const when = conversation.lastMessage
    ? activityLabel(
        conversation.lastActivityAt,
        now,
        view.locale,
        view.timeZone,
      )
    : "";
  const label = [
    `${conversation.kind === "group" ? copy.dm.sidebar.groupLabel : copy.dm.sidebar.directLabel}: ${title}`,
    presence ? presenceText(presence, copy) : null,
    badge
      ? pluralCopy(
          {
            one: copy.sidebar.unreadCount_one,
            other: copy.sidebar.unreadCount_other,
          },
          count,
        )
      : null,
  ]
    .filter(Boolean)
    .join(", ");

  return (
    <button
      type="button"
      onClick={() => view.onSelect(conversation.id)}
      aria-current={active ? "page" : undefined}
      aria-label={label}
      className={cn(
        "flex min-h-14 w-full items-center gap-3 rounded-lg px-2 py-1.5 text-start outline-none transition-colors focus-visible:ring-2 focus-visible:ring-focus-ring motion-reduce:transition-none",
        active
          ? "bg-state-selected text-state-selected-foreground"
          : "text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-foreground",
        muted && !active && "opacity-60",
      )}
    >
      <ConversationAvatar
        title={title}
        id={counterpart ?? conversation.id}
        group={conversation.kind === "group"}
        presence={presence}
        className="size-10"
      />
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="flex min-w-0 items-baseline gap-2">
          <span
            className={cn(
              "min-w-0 flex-1 truncate text-sm",
              unreadHere || active
                ? "font-semibold text-foreground"
                : "font-medium",
            )}
            title={title}
          >
            {title}
          </span>
          {when ? (
            <span
              aria-hidden="true"
              className={cn(
                "shrink-0 text-xs whitespace-nowrap tabular-nums",
                unreadHere
                  ? "font-semibold text-foreground"
                  : "text-muted-foreground",
              )}
            >
              {when}
            </span>
          ) : null}
        </span>
        <span className="flex min-w-0 items-center gap-2">
          <span
            className={cn(
              "min-w-0 flex-1 truncate text-xs",
              unreadHere
                ? "font-semibold text-foreground"
                : "text-muted-foreground",
            )}
            title={preview || undefined}
          >
            {preview}
          </span>
          {badge ? (
            <span
              aria-hidden="true"
              className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-brand-solid px-1.5 text-xs font-bold text-brand-solid-foreground tabular-nums"
            >
              {badge}
            </span>
          ) : null}
        </span>
      </span>
    </button>
  );
}

/** The initials of the other person, or the group glyph. */
export function ConversationAvatar({
  title,
  id,
  group,
  className,
  presence,
}: {
  readonly title: string;
  readonly id: string;
  readonly group: boolean;
  readonly className?: string;
  /** The presence dot at the corner; only a person (not a group) has one. */
  readonly presence?: ChatPresenceState;
}) {
  const avatar = (
    <Avatar className={cn("size-8 shrink-0", className)} aria-hidden="true">
      <AvatarFallback className={cn("text-xs font-semibold", avatarToneOf(id))}>
        {group ? <Users className="size-4" /> : initialsOf(title)}
      </AvatarFallback>
    </Avatar>
  );
  if (!presence) return avatar;
  return (
    <span className="relative inline-flex shrink-0">
      {avatar}
      <PresenceDot state={presence} />
    </span>
  );
}
