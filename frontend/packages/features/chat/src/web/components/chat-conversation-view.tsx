import { useMemo, useState } from "react";
import { ChevronLeft, EyeOff, Info, LogOut, Ban, UserPlus } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  conversationTitle,
  directCounterpart,
  groupRoomLeft,
} from "../../core/model/conversation-model";
import { CHAT_LIMITS, type ChatConversation } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { ConversationAvatar } from "./chat-conversation-list";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { ChatPersonPicker, type PickablePerson } from "./chat-person-picker";
import { fillCopy, pluralCopy } from "./use-chat-copy";

// ─── Header ─────────────────────────────────────────────────────────────────

export interface ChatConversationHeaderProps {
  readonly conversation: ChatConversation;
  readonly viewerId: string;
  readonly nameOf: (userId: string) => string;
  readonly copy: ChatCopy;
  readonly detailsOpen: boolean;
  readonly blocked: boolean;
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  /** Narrow screens: leaves the conversation for the list. */
  readonly onOpenList: () => void;
  readonly onToggleDetails: () => void;
  readonly onHide: () => void;
}

/** The header of an open conversation: who it is with, the details toggle and the hide action. */
export function ChatConversationHeader({
  conversation,
  viewerId,
  nameOf,
  copy,
  detailsOpen,
  blocked,
  presenceOf,
  onOpenList,
  onToggleDetails,
  onHide,
}: ChatConversationHeaderProps) {
  const title = conversationTitle(conversation, viewerId, nameOf);
  const counterpart = directCounterpart(conversation, viewerId);
  const group = conversation.kind === "group";
  return (
    <header className="flex min-h-14 flex-wrap items-center gap-x-2 gap-y-1 border-b border-border bg-background px-3 py-2 md:px-5">
      <Button
        variant="ghost"
        size="icon-sm"
        className="md:hidden"
        aria-label={copy.list.backToList}
        onClick={onOpenList}
      >
        <ChevronLeft aria-hidden="true" />
      </Button>
      <ConversationAvatar
        title={title}
        id={counterpart ?? conversation.id}
        group={group}
        presence={counterpart && !group ? presenceOf?.(counterpart) : undefined}
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <h1 className="flex min-w-0 items-center gap-2 text-base font-semibold text-foreground">
          <span className="truncate">{title}</span>
          {blocked ? (
            <span className="shrink-0 rounded-full border border-border px-2 py-0.5 text-xs font-medium text-muted-foreground">
              {copy.dm.details.blockedBadge}
            </span>
          ) : null}
        </h1>
        <p className="truncate text-xs text-muted-foreground">
          {group
            ? fillCopy(copy.dm.view.groupSubtitle, {
                count: conversation.participants.length,
              })
            : copy.dm.view.directSubtitle}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={copy.dm.view.hide}
          title={copy.dm.view.hide}
          onClick={onHide}
        >
          <EyeOff aria-hidden="true" />
        </Button>
        <Button
          variant={detailsOpen ? "secondary" : "ghost"}
          size="icon-sm"
          aria-label={copy.dm.view.details}
          title={copy.dm.view.details}
          aria-pressed={detailsOpen}
          onClick={onToggleDetails}
        >
          <Info aria-hidden="true" />
        </Button>
      </div>
    </header>
  );
}

// ─── Details panels ─────────────────────────────────────────────────────────

export interface ChatConversationDetailsProps {
  readonly conversation: ChatConversation;
  readonly viewerId: string;
  readonly nameOf: (userId: string) => string;
  readonly copy: ChatCopy;
  readonly locale: string;
  readonly blocked: boolean;
  readonly busy: boolean;
  /** The presence of a person; the member list shows a dot with it. */
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly onBlock: () => void;
  readonly onUnblock: () => void;
  readonly onAddPeople: () => void;
  readonly onLeave: () => void;
  readonly onHide: () => void;
}

function formatDay(iso: string, locale: string): string {
  const at = new Date(iso);
  return Number.isNaN(at.getTime())
    ? ""
    : at.toLocaleDateString(locale, { dateStyle: "medium" });
}

/** The profile of a direct conversation, or the details of a group, in the side panel. */
export function ChatConversationDetails(props: ChatConversationDetailsProps) {
  return props.conversation.kind === "direct" ? (
    <DmProfile {...props} />
  ) : (
    <GroupDetails {...props} />
  );
}

function DmProfile({
  conversation,
  viewerId,
  nameOf,
  copy,
  locale,
  blocked,
  busy,
  onBlock,
  onUnblock,
  onHide,
}: ChatConversationDetailsProps) {
  const personId = directCounterpart(conversation, viewerId);
  const name = personId ? nameOf(personId) : "";
  return (
    <div
      className="flex min-h-0 flex-1 flex-col overflow-y-auto"
      role="region"
      aria-label={copy.dm.details.dmTitle}
    >
      <div className="flex flex-col items-center gap-2 border-b border-border px-4 py-6 text-center">
        <Avatar className="size-16" aria-hidden="true">
          <AvatarFallback
            className={cn(
              "text-xl font-semibold",
              avatarToneOf(personId ?? conversation.id),
            )}
          >
            {initialsOf(name)}
          </AvatarFallback>
        </Avatar>
        <h2 className="text-lg font-semibold text-foreground">{name}</h2>
        {blocked ? (
          <span className="rounded-full border border-border px-2 py-0.5 text-xs font-medium text-muted-foreground">
            {copy.dm.details.blockedBadge}
          </span>
        ) : null}
      </div>
      <dl className="flex flex-col gap-1 border-b border-border px-4 py-3 text-sm">
        <div className="flex gap-3 py-1">
          <dt className="w-40 shrink-0 text-muted-foreground">
            {copy.dm.details.since}
          </dt>
          <dd className="min-w-0 text-foreground">
            {formatDay(conversation.createdAt, locale)}
          </dd>
        </div>
      </dl>
      <div className="flex flex-col gap-2 px-4 py-3">
        <Button
          type="button"
          variant="outline"
          onClick={onHide}
          disabled={busy}
        >
          <EyeOff aria-hidden="true" />
          {copy.dm.view.hide}
        </Button>
        {blocked ? (
          <Button
            type="button"
            variant="outline"
            onClick={onUnblock}
            disabled={busy}
          >
            <Ban aria-hidden="true" />
            {copy.dm.details.unblock}
          </Button>
        ) : (
          <Button
            type="button"
            variant="destructive"
            onClick={onBlock}
            disabled={busy}
          >
            <Ban aria-hidden="true" />
            {copy.dm.details.block}
          </Button>
        )}
      </div>
    </div>
  );
}

function GroupDetails({
  conversation,
  viewerId,
  nameOf,
  copy,
  busy,
  presenceOf,
  onAddPeople,
  onLeave,
  onHide,
}: ChatConversationDetailsProps) {
  const title = conversationTitle(conversation, viewerId, nameOf);
  const count = conversation.participants.length;
  const full = groupRoomLeft(count) === 0;
  return (
    <div
      className="flex min-h-0 flex-1 flex-col overflow-y-auto"
      role="region"
      aria-label={copy.dm.details.groupTitle}
    >
      <div className="flex flex-col items-center gap-2 border-b border-border px-4 py-6 text-center">
        <ConversationAvatar
          title={title}
          id={conversation.id}
          group
          className="size-16"
        />
        <h2 className="text-lg font-semibold text-foreground">{title}</h2>
        <p className="text-sm text-muted-foreground">
          {pluralCopy(
            {
              one: copy.dm.sidebar.participants_one,
              other: copy.dm.sidebar.participants_other,
            },
            count,
          )}
        </p>
      </div>
      <section
        aria-labelledby="chat-group-members"
        className="flex flex-col gap-1 border-b border-border px-4 py-3"
      >
        <h3
          id="chat-group-members"
          className="text-xs font-semibold uppercase tracking-wide text-muted-foreground"
        >
          {fillCopy(copy.dm.details.membersHeading, { count })}
        </h3>
        <ul className="flex flex-col">
          {conversation.participants.map((participant) => {
            const name = nameOf(participant.userId);
            const presence =
              participant.userId === viewerId
                ? undefined
                : presenceOf?.(participant.userId);
            return (
              <li
                key={participant.userId}
                className="flex min-h-11 items-center gap-3 py-1 text-sm"
              >
                <span className="relative inline-flex shrink-0">
                  <Avatar className="size-8" aria-hidden="true">
                    <AvatarFallback
                      className={cn(
                        "text-xs font-semibold",
                        avatarToneOf(participant.userId),
                      )}
                    >
                      {initialsOf(name)}
                    </AvatarFallback>
                  </Avatar>
                  {presence ? <PresenceDot state={presence} /> : null}
                </span>
                <span className="min-w-0 flex-1 truncate font-medium text-foreground">
                  {name}
                  {presence ? (
                    <span className="sr-only">
                      {`, ${presenceText(presence, copy)}`}
                    </span>
                  ) : null}
                </span>
                {participant.userId === viewerId ? (
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {copy.dm.details.you}
                  </span>
                ) : null}
              </li>
            );
          })}
        </ul>
      </section>
      <div className="flex flex-col gap-2 px-4 py-3">
        <Button
          type="button"
          variant="outline"
          onClick={onAddPeople}
          disabled={busy || full}
          aria-describedby={full ? "chat-group-full" : undefined}
        >
          <UserPlus aria-hidden="true" />
          {copy.dm.details.addPeople}
        </Button>
        {full ? (
          <p id="chat-group-full" className="text-xs text-muted-foreground">
            {fillCopy(copy.dm.details.addFull, { max: CHAT_LIMITS.groupCap })}
          </p>
        ) : null}
        <Button
          type="button"
          variant="outline"
          onClick={onHide}
          disabled={busy}
        >
          <EyeOff aria-hidden="true" />
          {copy.dm.view.hide}
        </Button>
        <Button
          type="button"
          variant="destructive"
          onClick={onLeave}
          disabled={busy}
        >
          <LogOut aria-hidden="true" />
          {copy.dm.details.leave}
        </Button>
      </div>
    </div>
  );
}

// ─── Add people ─────────────────────────────────────────────────────────────

export interface ChatAddPeopleDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  /** The Workspace members that are not yet in the group. */
  readonly candidates: readonly PickablePerson[];
  /** How many more people the group takes. */
  readonly roomLeft: number;
  readonly error: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (userIds: readonly string[]) => void;
}

/** Adds people to a group (UC-CHT-E03); any participant may. The picker stops at the room the group has left. */
export function ChatAddPeopleDialog(props: ChatAddPeopleDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent>
        <AddPeopleForm {...props} />
      </DialogContent>
    </Dialog>
  );
}

function AddPeopleForm({
  busy,
  candidates,
  roomLeft,
  error,
  copy,
  onOpenChange,
  onSubmit,
}: ChatAddPeopleDialogProps) {
  const [picked, setPicked] = useState<ReadonlySet<string>>(new Set());
  const ordered = useMemo(
    () => [...candidates].sort((a, b) => a.name.localeCompare(b.name)),
    [candidates],
  );
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (picked.size > 0 && !busy) onSubmit([...picked]);
      }}
    >
      <DialogHeader>
        <DialogTitle>{copy.dm.details.addTitle}</DialogTitle>
        <DialogDescription>
          {fillCopy(copy.dm.details.addHint, { max: CHAT_LIMITS.groupCap })}
        </DialogDescription>
      </DialogHeader>
      <ChatPersonPicker
        people={ordered}
        selected={picked}
        full={picked.size >= roomLeft}
        copy={copy}
        emptyText={copy.dm.details.addNobody}
        onToggle={(id, value) =>
          setPicked((current) => {
            const next = new Set(current);
            if (value) next.add(id);
            else next.delete(id);
            return next;
          })
        }
      />
      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
      <DialogFooter>
        <Button
          type="button"
          variant="ghost"
          onClick={() => onOpenChange(false)}
        >
          {copy.dm.details.cancel}
        </Button>
        <Button type="submit" disabled={picked.size === 0 || busy}>
          {copy.dm.details.addSubmit}
        </Button>
      </DialogFooter>
    </form>
  );
}
