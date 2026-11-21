import { useMemo, useState } from "react";
import { ChevronLeft, EyeOff, LogOut, Ban, UserPlus } from "lucide-react";
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
  DropdownMenuItem,
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
import { fillCopy } from "./use-chat-copy";
import { ChatHeaderActions, type ChatCallKind } from "./chat-header-actions";
import type { ChatInfoSectionKey } from "./chat-info-panel";

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
  /** Opens the message search (UC-CHT-H07). */
  readonly onSearch?: () => void;
  readonly callsEnabled?: boolean;
  readonly onStartCall?: (kind: ChatCallKind) => void;
  readonly onOpenSection: (key: ChatInfoSectionKey) => void;
}

/** The status line under a conversation's name: the people of a group, or the presence of a 1-1 partner. */
export function conversationSubtitle(
  conversation: ChatConversation,
  copy: ChatCopy,
  presence: ChatPresenceState | undefined,
): string {
  if (conversation.kind === "group") {
    return fillCopy(copy.dm.view.groupSubtitle, {
      count: conversation.participants.length,
    });
  }
  return presence === "online"
    ? copy.dm.view.activeNow
    : presence === "idle"
      ? copy.presence.idle
      : copy.dm.view.directSubtitle;
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
  onSearch,
  callsEnabled,
  onStartCall,
  onOpenSection,
}: ChatConversationHeaderProps) {
  const title = conversationTitle(conversation, viewerId, nameOf);
  const counterpart = directCounterpart(conversation, viewerId);
  const group = conversation.kind === "group";
  const presence =
    counterpart && !group ? presenceOf?.(counterpart) : undefined;
  const subtitle = conversationSubtitle(conversation, copy, presence);
  return (
    <header className="flex min-h-14 flex-wrap items-center gap-x-2.5 gap-y-1 border-b border-border bg-background px-3 py-2 md:px-4">
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
        presence={presence}
        className="size-9"
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <h1 className="truncate text-base font-semibold text-foreground">
          {title}
        </h1>
        <p className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
          {blocked ? (
            <span className="shrink-0 rounded-full border border-border px-1.5 text-xs font-medium">
              {copy.dm.details.blockedBadge}
            </span>
          ) : (
            <span className="truncate" data-testid="chat-conversation-subtitle">
              {subtitle}
            </span>
          )}
        </p>
      </div>
      <ChatHeaderActions
        copy={copy}
        callsEnabled={callsEnabled}
        onStartCall={onStartCall}
        onSearch={onSearch}
        infoOpen={detailsOpen}
        onToggleInfo={onToggleDetails}
        onOpenSection={onOpenSection}
        menu={
          <DropdownMenuItem onSelect={onHide}>
            <EyeOff aria-hidden="true" />
            {copy.dm.view.hide}
          </DropdownMenuItem>
        }
      />
    </header>
  );
}

// ─── Info panel pieces ──────────────────────────────────────────────────────

export interface ChatConversationInfoProps {
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

/** The people of a group, one row each, for the Members section of the info panel. */
export function ConversationMemberRows({
  conversation,
  viewerId,
  nameOf,
  copy,
  presenceOf,
}: Pick<
  ChatConversationInfoProps,
  "conversation" | "viewerId" | "nameOf" | "copy" | "presenceOf"
>) {
  return (
    <ul className="flex flex-col px-4">
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
  );
}

/** The buttons under the sections of a conversation's info panel: add people, hide, leave or block. */
export function ConversationInfoFooter({
  conversation,
  copy,
  locale,
  blocked,
  busy,
  onBlock,
  onUnblock,
  onAddPeople,
  onLeave,
  onHide,
}: ChatConversationInfoProps) {
  const group = conversation.kind === "group";
  const full = groupRoomLeft(conversation.participants.length) === 0;
  return (
    <>
      {group ? null : (
        <p className="flex gap-3 text-sm">
          <span className="w-40 shrink-0 text-muted-foreground">
            {copy.dm.details.since}
          </span>
          <span className="min-w-0 text-foreground">
            {formatDay(conversation.createdAt, locale)}
          </span>
        </p>
      )}
      {group ? (
        <>
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
        </>
      ) : null}
      <Button type="button" variant="outline" onClick={onHide} disabled={busy}>
        <EyeOff aria-hidden="true" />
        {copy.dm.view.hide}
      </Button>
      {group ? (
        <Button
          type="button"
          variant="destructive"
          onClick={onLeave}
          disabled={busy}
        >
          <LogOut aria-hidden="true" />
          {copy.dm.details.leave}
        </Button>
      ) : blocked ? (
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
    </>
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
