import { useId, useMemo, useState } from "react";
import {
  Check,
  ChevronLeft,
  ShieldCheck,
  ShieldOff,
  Users,
  UserMinus,
  UserPlus,
  X,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import type { ChatChannelMember } from "../../core/types/chat";
import type { ChatPresenceState } from "../../core/model/ephemeral-model";
import { PresenceDot, presenceText } from "./chat-presence";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy, pluralCopy } from "./use-chat-copy";
import type { PickablePerson } from "./chat-person-picker";

export interface ChatChannelMemberRowsProps {
  readonly members: readonly ChatChannelMember[];
  readonly viewerId: string;
  readonly authorName: (userId: string) => string;
  readonly presenceOf?: (userId: string) => ChatPresenceState;
  readonly copy: ChatCopy;
  /** Remove another member (a manager); without it no row has a Remove button. */
  readonly onRemoveMember?: (userId: string) => void;
  /** Make a member a manager or a manager a member (a manager); without it no row has the role button. */
  readonly onSetRole?: (userId: string, role: "manager" | "member") => void;
  /** Open the invite dialog (a manager of a live channel); without it the section has no Invite button. */
  readonly onInvite?: () => void;
}

/** The members of a channel, one row each, for the Members section of the info panel (it used to be a popover of the header). */
export function ChatChannelMemberRows({
  members,
  viewerId,
  authorName,
  presenceOf,
  copy,
  onRemoveMember,
  onInvite,
  onSetRole,
}: ChatChannelMemberRowsProps) {
  if (members.length === 0) {
    return (
      <p className="px-4 py-1 text-sm text-muted-foreground">
        {copy.header.membersEmpty}
      </p>
    );
  }
  return (
    <>
      {onInvite ? (
        <div className="px-4 pb-2">
          <Button variant="outline" size="sm" onClick={onInvite}>
            <UserPlus aria-hidden="true" />
            {copy.membership.invite}
          </Button>
        </div>
      ) : null}
      <ul className="flex flex-col px-4">
        {members.map((member) => {
          const name = authorName(member.userId);
          const presence =
            member.userId === viewerId
              ? undefined
              : presenceOf?.(member.userId);
          return (
            <li
              key={member.userId}
              className="flex min-h-11 items-center gap-3 py-1 text-sm"
            >
              <span className="relative inline-flex shrink-0">
                <Avatar className="size-8" aria-hidden="true">
                  <AvatarFallback
                    className={cn(
                      "text-xs font-semibold",
                      avatarToneOf(member.userId),
                    )}
                  >
                    {initialsOf(name)}
                  </AvatarFallback>
                </Avatar>
                {presence ? <PresenceDot state={presence} /> : null}
              </span>
              <span className="min-w-0 flex-1 truncate font-medium text-foreground">
                {name}
                {member.userId === viewerId ? ` (${copy.header.you})` : ""}
                {presence ? (
                  <span className="sr-only">
                    {`, ${presenceText(presence, copy)}`}
                  </span>
                ) : null}
              </span>
              {member.role === "manager" ? (
                <span className="text-xs text-muted-foreground">
                  {copy.header.manager}
                </span>
              ) : null}
              {onSetRole && member.userId !== viewerId ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={fillCopy(
                    member.role === "manager"
                      ? copy.membership.makeMember
                      : copy.membership.makeManager,
                    { name },
                  )}
                  onClick={() =>
                    onSetRole(
                      member.userId,
                      member.role === "manager" ? "member" : "manager",
                    )
                  }
                >
                  {member.role === "manager" ? (
                    <ShieldOff aria-hidden="true" />
                  ) : (
                    <ShieldCheck aria-hidden="true" />
                  )}
                </Button>
              ) : null}
              {onRemoveMember && member.userId !== viewerId ? (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={fillCopy(copy.membership.remove, { name })}
                  onClick={() => onRemoveMember(member.userId)}
                >
                  <UserMinus aria-hidden="true" />
                </Button>
              ) : null}
            </li>
          );
        })}
      </ul>
    </>
  );
}

export interface ChatInviteDialogProps {
  readonly open: boolean;
  readonly busy: boolean;
  readonly channelName: string;
  /** A private channel is joined by invitation only; the intro says so (the default). */
  readonly isPrivate?: boolean;
  /** The Workspace members that are not yet in the channel. */
  readonly candidates: readonly PickablePerson[];
  readonly error: string | null;
  /** Said inside the dialog after an invite that added some people and skipped others. */
  readonly notice?: string | null;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly onSubmit: (userIds: readonly string[]) => void;
}

/** Lower case without accents (đ included), so "thao" finds "Thảo". */
function fold(value: string): string {
  return value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

/** The given name of a Vietnamese full name (its last word), as the board's chips show it. */
function chipName(name: string): string {
  const words = name.trim().split(/\s+/);
  return words[words.length - 1] ?? name;
}

/**
 * Invites Workspace members into a channel. Chat-Var-Invite and -Invite-M draw it: a token field with the picked
 * people, the list grouped into members and guests with a check each, a warning for a guest, and the empty state.
 * Below 768 px it is a full page with a back button, as the phone board draws it.
 */
export function ChatInviteDialog(props: ChatInviteDialogProps) {
  return (
    <Dialog open={props.open} onOpenChange={props.onOpenChange}>
      <DialogContent
        showCloseButton={false}
        data-testid="chat-invite-dialog"
        className="gap-0 p-0 sm:max-w-[540px] max-md:inset-0 max-md:h-dvh max-md:max-w-none max-md:translate-x-0 max-md:translate-y-0 max-md:rounded-none max-md:ring-0"
      >
        <InviteForm {...props} />
      </DialogContent>
    </Dialog>
  );
}

function InviteForm({
  busy,
  channelName,
  isPrivate = true,
  candidates,
  error,
  notice,
  copy,
  onOpenChange,
  onSubmit,
}: ChatInviteDialogProps) {
  const text = copy.membership;
  const [picked, setPicked] = useState<readonly string[]>([]);
  const [query, setQuery] = useState("");
  const searchId = useId();
  const ordered = useMemo(
    () => [...candidates].sort((a, b) => a.name.localeCompare(b.name)),
    [candidates],
  );
  // A person who is no longer a candidate (they joined meanwhile) leaves the picked list.
  const chosen = ordered.filter((person) => picked.includes(person.id));
  const needle = fold(query.trim());
  const visible = ordered.filter(
    (person) =>
      needle.length === 0 ||
      fold(person.name).includes(needle) ||
      fold(person.detail ?? "").includes(needle),
  );
  const members = visible.filter((person) => !person.guest);
  const guests = visible.filter((person) => person.guest);
  const pickedGuests = chosen.filter((person) => person.guest);

  const toggle = (id: string) =>
    setPicked((current) =>
      current.includes(id)
        ? current.filter((entry) => entry !== id)
        : [...current, id],
    );

  const row = (person: PickablePerson) => {
    const on = chosen.some((entry) => entry.id === person.id);
    return (
      <li key={person.id}>
        <label
          className={cn(
            "flex min-h-12 cursor-pointer items-center gap-3 rounded-md px-2 py-1.5 text-sm hover:bg-muted",
            on && "bg-state-selected",
          )}
        >
          <Avatar className="size-8" aria-hidden="true">
            <AvatarFallback
              className={cn("text-xs font-semibold", avatarToneOf(person.id))}
            >
              {initialsOf(person.name)}
            </AvatarFallback>
          </Avatar>
          <span className="min-w-0 flex-1">
            <span className="block truncate font-semibold">{person.name}</span>
            {person.detail ? (
              <span className="block truncate text-xs text-muted-foreground">
                {person.detail}
              </span>
            ) : null}
          </span>
          <input
            type="checkbox"
            className="peer sr-only"
            checked={on}
            onChange={() => toggle(person.id)}
          />
          <span
            aria-hidden="true"
            className={cn(
              "grid size-5 place-items-center rounded-md border border-input text-primary-foreground peer-focus-visible:ring-2 peer-focus-visible:ring-ring",
              on && "border-primary bg-primary",
            )}
          >
            {on ? <Check className="size-3.5" /> : null}
          </span>
        </label>
      </li>
    );
  };

  return (
    <form
      className="flex min-h-0 flex-1 flex-col md:max-h-[85vh]"
      onSubmit={(event) => {
        event.preventDefault();
        if (chosen.length > 0 && !busy) onSubmit(chosen.map((p) => p.id));
      }}
    >
      <DialogHeader className="flex-row items-center gap-2 border-b border-border px-4 py-3 md:px-6 md:py-4">
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="md:hidden"
          aria-label={text.cancel}
          onClick={() => onOpenChange(false)}
        >
          <ChevronLeft aria-hidden="true" />
        </Button>
        <div className="min-w-0 flex-1">
          <DialogTitle className="max-md:sr-only">
            {fillCopy(text.inviteTitle, { channel: channelName })}
          </DialogTitle>
          <div aria-hidden="true" className="min-w-0 md:hidden">
            <div className="truncate text-base font-semibold">
              {fillCopy(text.inviteTitleShort, { channel: channelName })}
            </div>
            {isPrivate ? (
              <div className="text-xs text-muted-foreground">
                {text.invitePrivateLabel}
              </div>
            ) : null}
          </div>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="max-md:hidden"
          aria-label={text.cancel}
          onClick={() => onOpenChange(false)}
        >
          <X aria-hidden="true" />
        </Button>
      </DialogHeader>

      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 py-3 md:px-6">
        <DialogDescription className="max-md:hidden">
          {isPrivate
            ? fillCopy(text.inviteIntroPrivate, { channel: channelName })
            : text.inviteHint}
        </DialogDescription>
        <div
          role="group"
          aria-label={text.inviteSearchLabel}
          className="flex min-h-11 flex-wrap items-center gap-1.5 rounded-md border border-ring px-2.5 py-1.5"
        >
          {chosen.map((person) => (
            <span
              key={person.id}
              className="inline-flex items-center gap-1.5 rounded-full bg-muted py-0.5 ps-0.5 pe-1 text-sm"
            >
              <Avatar className="size-5" aria-hidden="true">
                <AvatarFallback
                  className={cn(
                    "text-micro font-semibold",
                    avatarToneOf(person.id),
                  )}
                >
                  {initialsOf(person.name)}
                </AvatarFallback>
              </Avatar>
              {chipName(person.name)}
              <button
                type="button"
                className="grid size-5 place-items-center rounded-full hover:bg-background"
                aria-label={fillCopy(text.inviteRemoveChip, {
                  name: person.name,
                })}
                onClick={() => toggle(person.id)}
              >
                <X className="size-3" aria-hidden="true" />
              </button>
            </span>
          ))}
          <label htmlFor={searchId} className="sr-only">
            {text.inviteSearchLabel}
          </label>
          <input
            id={searchId}
            type="text"
            value={query}
            autoComplete="off"
            placeholder={text.inviteSearchPlaceholder}
            className="min-w-32 flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Backspace" && query === "" && chosen.length) {
                const last = chosen[chosen.length - 1];
                if (last) toggle(last.id);
              }
            }}
          />
        </div>

        {pickedGuests.length > 0 ? (
          <p
            role="status"
            className="rounded-md bg-state-selected px-3 py-2 text-sm"
          >
            {fillCopy(text.inviteGuestWarning, {
              name: pickedGuests.map((person) => person.name).join(", "),
            })}
          </p>
        ) : null}
        {notice ? (
          <p
            role="status"
            className="rounded-md bg-state-selected px-3 py-2 text-sm"
          >
            {notice}
          </p>
        ) : null}

        {candidates.length === 0 ? (
          <div
            role="status"
            className="flex flex-col items-center gap-2 py-10 text-center"
          >
            <span className="grid size-12 place-items-center rounded-xl bg-muted text-muted-foreground">
              <Users className="size-5" aria-hidden="true" />
            </span>
            <h4 className="text-sm font-semibold">{text.inviteEmptyTitle}</h4>
            <p className="max-w-72 text-muted-foreground">
              {text.inviteNobody}
            </p>
          </div>
        ) : visible.length === 0 ? (
          <p role="status" className="py-3 text-sm text-muted-foreground">
            {copy.dm.dialog.empty}
          </p>
        ) : (
          <div className="flex flex-col">
            {[
              { key: "members", label: text.inviteGroupMembers, list: members },
              { key: "guests", label: text.inviteGroupGuests, list: guests },
            ]
              .filter((group) => group.list.length > 0)
              .map((group) => (
                <section key={group.key} aria-label={group.label}>
                  <h4 className="pt-3 pb-1 text-xs font-semibold tracking-wide text-muted-foreground uppercase">
                    {group.label}
                  </h4>
                  <ul className="flex flex-col gap-0.5">
                    {group.list.map(row)}
                  </ul>
                </section>
              ))}
          </div>
        )}
        {error ? (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        ) : null}
      </div>

      <div className="flex gap-2 border-t border-border px-4 py-3 md:justify-end md:px-6 md:py-4 max-md:flex-col">
        <Button
          type="button"
          variant="outline"
          className="max-md:hidden"
          onClick={() => onOpenChange(false)}
        >
          {text.inviteSkip}
        </Button>
        <Button
          type="submit"
          className="max-md:min-h-11"
          disabled={chosen.length === 0 || busy}
        >
          {chosen.length > 0
            ? pluralCopy(
                {
                  one: text.inviteSubmitCount_one,
                  other: text.inviteSubmitCount_other,
                },
                chosen.length,
              )
            : text.inviteSubmit}
        </Button>
      </div>
    </form>
  );
}
