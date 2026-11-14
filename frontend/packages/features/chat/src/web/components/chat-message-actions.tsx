import { useState, type KeyboardEvent, type ReactNode } from "react";
import { MoreHorizontal, Reply, type LucideIcon } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetTitle,
  cn,
} from "@notrelix/ui-web";
import { CHAT_REACTION_PALETTE } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { ReactionPicker } from "./chat-reactions";
import { fillCopy } from "./use-chat-copy";

/** One entry of the More menu (and of the touch sheet). */
export interface MessageMenuItem {
  readonly key: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly onSelect: () => void;
  /** Destructive entries sit last, after a separator, in the destructive colour. */
  readonly destructive?: boolean;
}

export interface MessageActionBarProps {
  readonly copy: ChatCopy;
  /** The accessible name of the toolbar ("Message actions for …"). */
  readonly label: string;
  /**
   * Shown or faded out. A faded bar stays in the page and in the tab order (one Tab stop), so the keyboard
   * reaches it; focusing it shows it.
   */
  readonly visible: boolean;
  /** The side of the bubble the bar sits on: toward the middle of the pane. */
  readonly side: "start" | "end";
  /** "side" beside the bubble; "top" above it when the bubble leaves no room beside it. */
  readonly placement: "side" | "top";
  /** The clock time of the message, shown muted at the outer end of the bar. */
  readonly time: ReactNode;
  readonly canReact: boolean;
  readonly reacted: readonly string[];
  readonly onPick: (emoji: string) => void;
  readonly onReply?: () => void;
  readonly items: readonly MessageMenuItem[];
  /** A menu or the reaction popover is open: the bar stays visible until it closes. */
  readonly onHoldChange: (held: boolean) => void;
}

/**
 * The floating action bar of one message: React, Reply and More, anchored to the bubble on the side that faces
 * the middle of the pane. One Tab stop: the arrow keys, Home and End move between its buttons.
 */
export function MessageActionBar({
  copy,
  label,
  visible,
  side,
  placement,
  time,
  canReact,
  reacted,
  onPick,
  onReply,
  items,
  onHoldChange,
}: MessageActionBarProps) {
  const [focusIndex, setFocusIndex] = useState(0);
  const count =
    (canReact ? 1 : 0) + (onReply ? 1 : 0) + (items.length > 0 ? 1 : 0);
  if (count === 0) return null;
  const current = Math.min(focusIndex, count - 1);
  const tab = (index: number) => (index === current ? 0 : -1);

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
    const buttons = Array.from(
      event.currentTarget.querySelectorAll<HTMLButtonElement>(
        "button[data-bar-button]",
      ),
    ).filter((button) => !button.disabled);
    if (buttons.length === 0) return;
    const at = buttons.indexOf(document.activeElement as HTMLButtonElement);
    let next = at < 0 ? 0 : at;
    const rtl =
      typeof getComputedStyle === "function" &&
      getComputedStyle(event.currentTarget).direction === "rtl";
    const forward = rtl ? "ArrowLeft" : "ArrowRight";
    if (event.key === forward) next = (next + 1) % buttons.length;
    else if (event.key === "ArrowLeft" || event.key === "ArrowRight")
      next = (next - 1 + buttons.length) % buttons.length;
    if (event.key === "Home") next = 0;
    if (event.key === "End") next = buttons.length - 1;
    event.preventDefault();
    setFocusIndex(next);
    buttons[next]?.focus();
  }

  let index = 0;
  const reactIndex = canReact ? index++ : -1;
  const replyIndex = onReply ? index++ : -1;
  const moreIndex = items.length > 0 ? index++ : -1;
  const safe = items.filter((item) => !item.destructive);
  const destructive = items.filter((item) => item.destructive);
  const timeLabel = (
    <span className="px-2 text-xs whitespace-nowrap tabular-nums text-muted-foreground">
      {time}
    </span>
  );

  return (
    <div
      role="toolbar"
      aria-label={label}
      data-visible={visible ? "true" : "false"}
      data-side={side}
      data-placement={placement}
      onKeyDown={handleKeyDown}
      className={cn(
        "absolute z-10 flex items-center gap-0.5 whitespace-nowrap rounded-full border border-border bg-popover p-0.5 text-popover-foreground shadow-md transition-opacity duration-100 motion-reduce:transition-none pointer-coarse:hidden",
        placement === "side" ? "top-1/2 -translate-y-1/2" : "bottom-full mb-1",
        placement === "side" && side === "start" && "end-full me-2",
        placement === "side" && side === "end" && "start-full ms-2",
        placement === "top" && side === "start" && "end-0",
        placement === "top" && side === "end" && "start-0",
        visible ? "visible opacity-100" : "pointer-events-none opacity-0",
      )}
    >
      {side === "start" ? timeLabel : null}
      {canReact ? (
        <ReactionPicker
          copy={copy}
          reacted={reacted}
          onPick={onPick}
          onOpenChange={onHoldChange}
          triggerProps={{
            tabIndex: tab(reactIndex),
            "data-bar-button": "",
            className: "rounded-full",
            onFocus: () => setFocusIndex(reactIndex),
          }}
        />
      ) : null}
      {onReply ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="rounded-full"
          aria-label={copy.toolbar.reply}
          title={copy.toolbar.reply}
          tabIndex={tab(replyIndex)}
          data-bar-button=""
          onFocus={() => setFocusIndex(replyIndex)}
          onClick={onReply}
        >
          <Reply aria-hidden="true" />
        </Button>
      ) : null}
      {items.length > 0 ? (
        <DropdownMenu onOpenChange={onHoldChange}>
          <DropdownMenuTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="rounded-full"
              aria-label={copy.toolbar.moreActions}
              title={copy.toolbar.moreActions}
              tabIndex={tab(moreIndex)}
              data-bar-button=""
              onFocus={() => setFocusIndex(moreIndex)}
            >
              <MoreHorizontal aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            align={side === "start" ? "end" : "start"}
            className="min-w-52"
          >
            {safe.map((item) => (
              <MenuEntry key={item.key} item={item} />
            ))}
            {safe.length > 0 && destructive.length > 0 ? (
              <DropdownMenuSeparator />
            ) : null}
            {destructive.map((item) => (
              <MenuEntry key={item.key} item={item} />
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null}
      {side === "end" ? timeLabel : null}
    </div>
  );
}

function MenuEntry({ item }: { readonly item: MessageMenuItem }) {
  const Icon = item.icon;
  return (
    <DropdownMenuItem
      variant={item.destructive ? "destructive" : "default"}
      onSelect={item.onSelect}
    >
      <Icon aria-hidden="true" />
      {item.label}
    </DropdownMenuItem>
  );
}

export interface MessageActionSheetProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly copy: ChatCopy;
  /** Who wrote it and a one-line snippet, so the sheet says which message it acts on. */
  readonly who: string;
  readonly snippet: string;
  readonly canReact: boolean;
  readonly reacted: readonly string[];
  readonly onPick: (emoji: string) => void;
  readonly onReply?: () => void;
  readonly items: readonly MessageMenuItem[];
}

/** The touch version of the action bar: a bottom sheet with the quick reactions and the action list (44 px rows). */
export function MessageActionSheet({
  open,
  onOpenChange,
  copy,
  who,
  snippet,
  canReact,
  reacted,
  onPick,
  onReply,
  items,
}: MessageActionSheetProps) {
  const entries: MessageMenuItem[] = [
    ...(onReply
      ? [
          {
            key: "reply",
            label: copy.toolbar.reply,
            icon: Reply,
            onSelect: onReply,
          },
        ]
      : []),
    ...items.filter((item) => !item.destructive),
    ...items.filter((item) => item.destructive),
  ];
  function run(action: () => void) {
    onOpenChange(false);
    action();
  }
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        className="gap-0 rounded-t-2xl pb-[max(env(safe-area-inset-bottom),0.75rem)]"
        data-testid="chat-message-sheet"
      >
        <div
          className="mx-auto mt-2 mb-1 h-1 w-10 rounded-full bg-border"
          aria-hidden="true"
        />
        <SheetTitle className="sr-only">{copy.toolbar.sheetLabel}</SheetTitle>
        <SheetDescription className="truncate px-4 pt-1 pb-2 text-xs text-muted-foreground">
          {who}: {snippet}
        </SheetDescription>
        {canReact ? (
          <div
            role="group"
            aria-label={copy.reactions.pickerLabel}
            className="flex items-center justify-between gap-1 border-b border-border px-3 pb-3"
          >
            {CHAT_REACTION_PALETTE.map((emoji) => {
              const mine = reacted.includes(emoji);
              return (
                <button
                  key={emoji}
                  type="button"
                  aria-pressed={mine}
                  aria-label={
                    mine
                      ? fillCopy(copy.reactions.unpick, { emoji })
                      : fillCopy(copy.reactions.pick, { emoji })
                  }
                  onClick={() => run(() => onPick(emoji))}
                  className={cn(
                    "flex size-11 items-center justify-center rounded-full text-2xl outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    mine ? "bg-state-selected" : "bg-muted",
                  )}
                >
                  <span aria-hidden="true">{emoji}</span>
                </button>
              );
            })}
          </div>
        ) : null}
        <ul className="flex flex-col py-1">
          {entries.map((item) => {
            const Icon = item.icon;
            return (
              <li key={item.key}>
                <button
                  type="button"
                  onClick={() => run(item.onSelect)}
                  className={cn(
                    "flex min-h-11 w-full items-center gap-3 px-4 text-start text-sm outline-none hover:bg-muted focus-visible:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-inset",
                    item.destructive ? "text-destructive" : "text-foreground",
                  )}
                >
                  <Icon className="size-5 shrink-0" aria-hidden="true" />
                  {item.label}
                </button>
              </li>
            );
          })}
        </ul>
      </SheetContent>
    </Sheet>
  );
}
