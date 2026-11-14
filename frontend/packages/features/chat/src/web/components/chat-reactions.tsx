import { useState, type ComponentProps } from "react";
import { ArrowLeft, Plus, SmilePlus, Users } from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  CHAT_REACTION_PALETTE,
  type ChatReactionSummary,
} from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";
import { EmojiPicker } from "./chat-emoji-picker";

export interface ReactionChipsProps {
  readonly messageId: string;
  readonly workspaceId: string;
  readonly reactions: readonly ChatReactionSummary[];
  readonly hooks: ChatHooks;
  readonly authorName: (userId: string) => string;
  readonly copy: ChatCopy;
  readonly onToggle: (emoji: string, add: boolean) => void;
  /** The side of the bubble the pills hug: the end for the caller's own message, the start otherwise. */
  readonly align?: "start" | "end";
}

/**
 * The reaction summary of a message: compact pills below the bubble with a small gap, never overlapping it (Messenger
 * style), one toggle per emoji (aria-pressed says whether the caller reacted; the count shows above one),
 * and a "who reacted" popover.
 */
export function ReactionChips({
  messageId,
  workspaceId,
  reactions,
  hooks,
  authorName,
  copy,
  onToggle,
  align = "start",
}: ReactionChipsProps) {
  if (reactions.length === 0) return null;
  return (
    <div
      role="group"
      aria-label={copy.reactions.pickerLabel}
      className={cn(
        "relative z-0 mt-1 flex max-w-full flex-wrap items-center gap-1",
        align === "end" ? "justify-end pe-2" : "justify-start ps-2",
      )}
      data-testid="chat-reactions"
      data-align={align}
    >
      {reactions.map((reaction) => {
        const label = fillCopy(
          reaction.reactedByMe ? copy.reactions.chipMine : copy.reactions.chip,
          { emoji: reaction.emoji, count: reaction.count },
        );
        return (
          <button
            key={reaction.emoji}
            type="button"
            aria-pressed={reaction.reactedByMe}
            aria-label={label}
            title={
              reaction.reactedByMe
                ? fillCopy(copy.reactions.unpick, { emoji: reaction.emoji })
                : fillCopy(copy.reactions.pick, { emoji: reaction.emoji })
            }
            onClick={() => onToggle(reaction.emoji, !reaction.reactedByMe)}
            className={cn(
              "inline-flex h-6 items-center gap-1 rounded-full border px-1.5 text-xs font-medium tabular-nums shadow-sm ring-2 ring-background transition-colors focus-visible:ring-focus-ring focus-visible:outline-none motion-reduce:transition-none",
              reaction.reactedByMe
                ? "border-primary bg-state-selected text-state-selected-foreground"
                : "border-border bg-popover text-popover-foreground hover:bg-muted",
            )}
          >
            <span aria-hidden="true" className="text-sm leading-none">
              {reaction.emoji}
            </span>
            {reaction.count > 1 || reaction.reactedByMe ? (
              <span>{reaction.count}</span>
            ) : (
              <span className="sr-only">{reaction.count}</span>
            )}
          </button>
        );
      })}
      <WhoReactedPopover
        messageId={messageId}
        workspaceId={workspaceId}
        reactions={reactions}
        hooks={hooks}
        authorName={authorName}
        copy={copy}
      />
    </div>
  );
}

function WhoReactedPopover({
  messageId,
  workspaceId,
  reactions,
  hooks,
  authorName,
  copy,
}: Omit<ReactionChipsProps, "onToggle" | "reactions"> & {
  readonly reactions: readonly ChatReactionSummary[];
}) {
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-xs"
          className="rounded-full text-muted-foreground opacity-70 hover:opacity-100 focus-visible:opacity-100"
          aria-label={copy.reactions.who}
          title={copy.reactions.who}
        >
          <Users aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-2">
        {open ? (
          <ReactorList
            messageId={messageId}
            workspaceId={workspaceId}
            reactions={reactions}
            hooks={hooks}
            authorName={authorName}
            copy={copy}
          />
        ) : null}
      </PopoverContent>
    </Popover>
  );
}

function ReactorList({
  messageId,
  workspaceId,
  reactions,
  hooks,
  authorName,
  copy,
}: Omit<ReactionChipsProps, "onToggle" | "reactions"> & {
  readonly reactions: readonly ChatReactionSummary[];
}) {
  const [kind, setKind] = useState<string | undefined>(undefined);
  const reactors = hooks.useReactors(workspaceId, messageId, kind, true);
  const items = reactors.data?.pages.flatMap((page) => page.items) ?? [];
  return (
    <div className="flex flex-col gap-2">
      <p id={`who-${messageId}`} className="text-sm font-semibold">
        {copy.reactions.whoLabel}
      </p>
      <div
        role="group"
        aria-label={copy.reactions.whoLabel}
        className="flex flex-wrap gap-1"
      >
        <Button
          type="button"
          size="xs"
          variant={kind === undefined ? "secondary" : "ghost"}
          aria-pressed={kind === undefined}
          onClick={() => setKind(undefined)}
        >
          {copy.reactions.whoAll}
        </Button>
        {reactions.map((reaction) => (
          <Button
            key={reaction.emoji}
            type="button"
            size="xs"
            variant={kind === reaction.emoji ? "secondary" : "ghost"}
            aria-pressed={kind === reaction.emoji}
            aria-label={`${reaction.emoji} ${reaction.count}`}
            onClick={() => setKind(reaction.emoji)}
          >
            <span aria-hidden="true">{reaction.emoji}</span>
            {reaction.count}
          </Button>
        ))}
      </div>
      {reactors.isError ? (
        <p role="alert" className="text-xs text-destructive">
          {copy.reactions.whoError}
        </p>
      ) : null}
      {!reactors.isPending && items.length === 0 && !reactors.isError ? (
        <p className="text-xs text-muted-foreground">
          {copy.reactions.whoEmpty}
        </p>
      ) : null}
      <ul
        aria-labelledby={`who-${messageId}`}
        className="flex max-h-60 flex-col gap-1 overflow-y-auto"
      >
        {items.map((item) => (
          <li
            key={`${item.emoji}-${item.userId}-${item.reactedAt}`}
            className="flex items-center justify-between gap-2 text-sm"
          >
            <span className="truncate">{authorName(item.userId)}</span>
            <span aria-hidden="true">{item.emoji}</span>
          </li>
        ))}
      </ul>
      {reactors.hasNextPage ? (
        <Button
          type="button"
          size="sm"
          variant="ghost"
          disabled={reactors.isFetchingNextPage}
          onClick={() => void reactors.fetchNextPage()}
        >
          {copy.reactions.loadMore}
        </Button>
      ) : null}
    </div>
  );
}

export interface ReactionPickerProps {
  readonly copy: ChatCopy;
  readonly onPick: (emoji: string) => void;
  readonly reacted: readonly string[];
  /** Told when the popover opens and closes (the message action bar stays visible while it is open). */
  readonly onOpenChange?: (open: boolean) => void;
  /** Extra props of the trigger button (the action bar's roving tab stop). */
  readonly triggerProps?: Partial<ComponentProps<typeof Button>> & {
    readonly "data-bar-button"?: string;
  };
}

/**
 * The reaction picker of a message toolbar: the quick row of six palette emoji, and a "more" button that
 * opens the full emoji picker in the same popover. Each is a button; a pick the caller already has is a remove, shown pressed.
 */
export function ReactionPicker({
  copy,
  onPick,
  reacted,
  onOpenChange,
  triggerProps,
}: ReactionPickerProps) {
  const [open, setOpenState] = useState(false);
  const [full, setFull] = useState(false);
  function setOpen(next: boolean) {
    setOpenState(next);
    onOpenChange?.(next);
  }
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setFull(false);
      }}
    >
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={copy.toolbar.react}
          title={copy.toolbar.react}
          {...triggerProps}
        >
          <SmilePlus aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="center"
        side="top"
        className={cn(
          "p-1.5 duration-100 motion-reduce:animate-none",
          full ? "w-80 gap-2" : "w-auto rounded-full",
        )}
      >
        {full ? (
          <>
            <Button
              type="button"
              variant="ghost"
              size="xs"
              className="self-start"
              onClick={() => setFull(false)}
            >
              <ArrowLeft aria-hidden="true" />
              {copy.emoji.back}
            </Button>
            <EmojiPicker
              copy={copy}
              pressed={reacted}
              label={copy.reactions.pickerLabel}
              onPick={(emoji) => {
                onPick(emoji);
                setOpen(false);
                setFull(false);
              }}
            />
          </>
        ) : (
          <div className="flex items-center gap-0.5">
            <div
              role="group"
              aria-label={copy.reactions.pickerLabel}
              className="flex items-center gap-0.5"
            >
              {CHAT_REACTION_PALETTE.map((emoji) => {
                const mine = reacted.includes(emoji);
                return (
                  <Button
                    key={emoji}
                    type="button"
                    variant={mine ? "secondary" : "ghost"}
                    size="icon-sm"
                    aria-pressed={mine}
                    aria-label={
                      mine
                        ? fillCopy(copy.reactions.unpick, { emoji })
                        : fillCopy(copy.reactions.pick, { emoji })
                    }
                    onClick={() => {
                      onPick(emoji);
                      setOpen(false);
                    }}
                  >
                    <span aria-hidden="true" className="text-base">
                      {emoji}
                    </span>
                  </Button>
                );
              })}
            </div>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={copy.emoji.more}
              title={copy.emoji.more}
              onClick={() => setFull(true)}
            >
              <Plus aria-hidden="true" />
            </Button>
          </div>
        )}
      </PopoverContent>
    </Popover>
  );
}
