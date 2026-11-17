import { useState, type ComponentProps } from "react";
import { ArrowLeft, Plus, SmilePlus } from "lucide-react";
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
  /** The bubble corner the pill sits on: the end for the caller's own message, the start otherwise (the other side). */
  readonly align?: "start" | "end";
}

/** Distinct emoji the pill shows before it only counts. */
const PILL_EMOJI = 3;

/**
 * The reaction summary of a message (Messenger style): one pill with up to three distinct emoji and the total count,
 * laid over the bubble's bottom corner (the far corner from the author's side) and hanging 10 px below it, with a ring in
 * the page colour so it reads as sitting on the bubble. Render it inside the bubble's positioned box. It is one button: its
 * label lists every emoji with its count, and it opens the "who reacted" popover. It is highlighted when the caller reacted.
 */
export function ReactionChips({
  messageId,
  workspaceId,
  reactions,
  hooks,
  authorName,
  copy,
  align = "start",
}: ReactionChipsProps) {
  const [open, setOpen] = useState(false);
  if (reactions.length === 0) return null;
  const total = reactions.reduce((sum, reaction) => sum + reaction.count, 0);
  const mine = reactions.some((reaction) => reaction.reactedByMe);
  const summary = reactions
    .map((reaction) =>
      fillCopy(
        reaction.reactedByMe ? copy.reactions.chipMine : copy.reactions.chip,
        {
          emoji: reaction.emoji,
          count: reaction.count,
        },
      ),
    )
    .join("; ");
  return (
    <div
      className={cn(
        "absolute top-full z-10 -mt-3",
        align === "end" ? "start-2" : "end-2",
      )}
      data-testid="chat-reactions"
      data-align={align}
    >
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={`${copy.reactions.who}: ${summary}`}
            aria-haspopup="dialog"
            data-mine={mine ? "true" : undefined}
            className={cn(
              "inline-flex h-[22px] items-center gap-[3px] rounded-full border px-1.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap tabular-nums ring-2 ring-background transition-colors focus-visible:ring-focus-ring focus-visible:outline-none motion-reduce:transition-none",
              mine
                ? "border-primary bg-state-selected text-state-selected-foreground"
                : "border-input bg-card text-muted-foreground hover:bg-muted",
            )}
          >
            <span aria-hidden="true" className="leading-none">
              {reactions
                .slice(0, PILL_EMOJI)
                .map((reaction) => reaction.emoji)
                .join("")}
            </span>
            <span aria-hidden="true">{total}</span>
          </button>
        </PopoverTrigger>
        <PopoverContent
          align={align === "end" ? "start" : "end"}
          className="w-64 p-2"
        >
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
    </div>
  );
}

function ReactorList({
  messageId,
  workspaceId,
  reactions,
  hooks,
  authorName,
  copy,
}: Omit<ReactionChipsProps, "reactions"> & {
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
