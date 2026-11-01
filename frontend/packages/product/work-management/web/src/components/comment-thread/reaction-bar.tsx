import { useState } from "react";
import { SmilePlus } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  cn,
} from "@notrelix/ui-web";
import { REACTION_PALETTE } from "@notrelix/work-management-core";
import type { CardReaction } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type { ThreadCopy } from "./comment-thread.copy";

const CHIP_CLASS =
  "inline-flex h-6 items-center gap-1 rounded-full border px-2 text-crumb outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

/**
 * The emoji chips of one comment and, for someone who may react, the button
 * that opens the six-emoji palette. A chip is a toggle: `aria-pressed` says
 * whether the viewer chose it, and its tooltip names who did. Without
 * `onToggle` (a viewer) the chips stay readable and inert.
 */
export function ReactionBar({
  reactions,
  copy,
  onToggle,
}: {
  reactions: readonly CardReaction[];
  copy: ThreadCopy;
  /** Adds (`on`) or withdraws the viewer's emoji; absent: the viewer cannot react. */
  onToggle?: (emoji: string, on: boolean) => void;
}) {
  const [paletteOpen, setPaletteOpen] = useState(false);
  const held = (emoji: string) =>
    reactions.some((r) => r.emoji === emoji && r.reactedByMe);

  return (
    <div
      role="group"
      aria-label={copy.reactions}
      className="flex flex-wrap items-center gap-1.5"
    >
      {reactions.map((reaction) => {
        const label = fillSheetCopy(
          reaction.reactedByMe ? copy.chipMine : copy.chip,
          { emoji: reaction.emoji, count: reaction.count },
        );
        return (
          <Tooltip key={reaction.emoji}>
            <TooltipTrigger asChild>
              <button
                type="button"
                aria-pressed={reaction.reactedByMe}
                aria-disabled={onToggle ? undefined : true}
                aria-label={label}
                onClick={() =>
                  onToggle?.(reaction.emoji, !reaction.reactedByMe)
                }
                className={cn(
                  CHIP_CLASS,
                  reaction.reactedByMe
                    ? "border-primary bg-state-selected text-foreground"
                    : "border-line bg-transparent text-muted-foreground hover:bg-state-hover-strong",
                )}
              >
                <span aria-hidden="true">{reaction.emoji}</span>
                <span aria-hidden="true">{reaction.count}</span>
              </button>
            </TooltipTrigger>
            <TooltipContent>
              {fillSheetCopy(copy.reactedBy, {
                names: reaction.users.map((user) => user.name).join(", "),
              })}
            </TooltipContent>
          </Tooltip>
        );
      })}
      {onToggle ? (
        <Popover open={paletteOpen} onOpenChange={setPaletteOpen}>
          <PopoverTrigger asChild>
            <button
              type="button"
              aria-label={copy.addReaction}
              className={cn(
                CHIP_CLASS,
                "h-5 border-transparent bg-transparent px-1.5 text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
              )}
            >
              <SmilePlus aria-hidden="true" className="size-3.5" />
            </button>
          </PopoverTrigger>
          <PopoverContent align="start" className="flex w-auto gap-1 p-1.5">
            {REACTION_PALETTE.map((emoji) => (
              <button
                key={emoji}
                type="button"
                aria-pressed={held(emoji)}
                aria-label={fillSheetCopy(copy.pick, { emoji })}
                onClick={() => {
                  onToggle(emoji, !held(emoji));
                  setPaletteOpen(false);
                }}
                className={cn(
                  "grid size-8 place-items-center rounded-md border-0 text-base outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
                  held(emoji) ? "bg-state-selected" : "bg-transparent",
                )}
              >
                {emoji}
              </button>
            ))}
          </PopoverContent>
        </Popover>
      ) : null}
    </div>
  );
}
