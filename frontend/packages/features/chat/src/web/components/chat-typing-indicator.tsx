import { useEffect, useState } from "react";
import { Avatar, AvatarFallback, cn } from "@notrelix/ui-web";
import { prefersReducedMotion } from "./animate-scroll";
import type { ChatCopy } from "./chat.copy";
import { avatarToneOf, initialsOf } from "./chat-initials";
import { fillCopy } from "./use-chat-copy";

/** "A is typing…", "A and B are typing…", "Several people are typing…". */
export function typingText(names: readonly string[], copy: ChatCopy): string {
  if (names.length === 0) return "";
  if (names.length === 1) return fillCopy(copy.typing.one, { name: names[0]! });
  if (names.length === 2) {
    return fillCopy(copy.typing.two, { first: names[0]!, second: names[1]! });
  }
  return copy.typing.several;
}

/**
 * The typing status for assistive technology: a polite, atomic live region that
 * is always in the page (so a screen reader tracks it). The visible cue is the
 * typing bubble at the end of the list.
 */
export function ChatTypingIndicator({
  names,
  copy,
}: {
  readonly names: readonly string[];
  readonly copy: ChatCopy;
}) {
  const text = typingText(names, copy);
  return (
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      data-testid="chat-typing-indicator"
      className="sr-only"
    >
      {text}
    </div>
  );
}

/** The three dots; their stagger lives in the .chat-typing-wave rule (globals.css). */
const DOTS = [0, 1, 2];

/** The three wave dots, shared by the bubble and the sidebar rows. */
export function ChatTypingDots({ className }: { readonly className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn("inline-flex items-center gap-1", className)}
    >
      {DOTS.map((dot) => (
        <span
          key={dot}
          className="chat-typing-wave size-1.5 rounded-full bg-muted-foreground"
        />
      ))}
    </span>
  );
}

/** How long the typing bubble takes to open or to close. The list follows the end for as long. */
export const TYPING_SLOT_MS = 180;

/** Who the typing bubble shows, and whether it is closing. */
export interface TypingSlot {
  readonly ids: readonly string[];
  readonly leaving: boolean;
}

interface SlotState extends TypingSlot {
  readonly space: string;
  readonly newestKey: string;
}

/**
 * The typing bubble's place at the end of the list. When the people stop typing it stays for TYPING_SLOT_MS while it
 * closes, so the list shrinks instead of losing a row in one frame. When one of them has just sent a message it goes at
 * once: the message takes its place. Another space, or reduced motion, drops it at once.
 */
export function useTypingSlot(
  typingIds: readonly string[],
  space: string,
  newest: { readonly key: string; readonly authorId: string | null },
): TypingSlot {
  const [slot, setSlot] = useState<SlotState>(() => ({
    ids: typingIds,
    leaving: false,
    space,
    newestKey: newest.key,
  }));
  let next = slot;
  if (space !== slot.space) {
    next = { ids: typingIds, leaving: false, space, newestKey: newest.key };
  } else if (typingIds.length > 0) {
    if (slot.leaving || slot.ids !== typingIds || slot.newestKey !== newest.key)
      next = { ids: typingIds, leaving: false, space, newestKey: newest.key };
  } else if (slot.ids.length > 0 && !slot.leaving) {
    const handedOver =
      newest.key !== slot.newestKey &&
      newest.authorId !== null &&
      slot.ids.includes(newest.authorId);
    next =
      handedOver || prefersReducedMotion()
        ? { ids: [], leaving: false, space, newestKey: newest.key }
        : { ...slot, leaving: true };
  }
  if (next !== slot) setSlot(next);

  useEffect(() => {
    if (!next.leaving) return;
    const timer = window.setTimeout(
      () =>
        setSlot((current) =>
          current.leaving ? { ...current, ids: [], leaving: false } : current,
        ),
      TYPING_SLOT_MS,
    );
    return () => window.clearTimeout(timer);
  }, [next.leaving]);

  return next;
}

/**
 * The typing bubble at the end of the list (Messenger): the first typer's avatar and a bubble of three dots
 * that rise and fade in a soft wave (still under reduced motion). Decorative: the live region above the composer says who is typing.
 * It opens by growing its height from nothing (and closes the same way while `leaving`), so the rows above glide up and
 * down with it instead of jumping by a whole row.
 */
export function ChatTypingBubble({
  ids,
  authorName,
  copy,
  leaving = false,
}: {
  readonly ids: readonly string[];
  readonly authorName: (userId: string) => string;
  readonly copy: ChatCopy;
  readonly leaving?: boolean;
}) {
  const [opened, setOpened] = useState(false);
  useEffect(() => {
    const frame = requestAnimationFrame(() => setOpened(true));
    return () => cancelAnimationFrame(frame);
  }, []);
  const first = ids[0];
  if (!first) return null;
  const open = opened && !leaving;
  const names = ids.map((id) => authorName(id));
  return (
    <div
      className={cn(
        "grid motion-safe:transition-[grid-template-rows,opacity] motion-safe:duration-[180ms] motion-safe:[transition-timing-function:var(--motion-ease-out)]",
        open ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0",
      )}
      data-testid="chat-typing-bubble"
      data-state={open ? "open" : "closed"}
      title={typingText(names, copy)}
    >
      <div className="min-h-0 overflow-hidden">
        <div className="flex items-end gap-2 px-3.5 pt-3 md:px-6 xl:px-5">
          <span className="relative inline-flex w-7 shrink-0">
            <Avatar className="size-7">
              <AvatarFallback
                className={cn("text-xs font-semibold", avatarToneOf(first))}
              >
                {initialsOf(names[0] ?? "")}
              </AvatarFallback>
            </Avatar>
          </span>
          <span
            className={cn(
              "inline-flex h-9 origin-bottom-left items-center rounded-2xl bg-sidebar-accent px-3.5 motion-safe:transition-transform motion-safe:duration-[180ms] motion-safe:[transition-timing-function:var(--motion-ease-out)]",
              open ? "scale-100" : "scale-90",
            )}
          >
            <ChatTypingDots />
          </span>
        </div>
      </div>
    </div>
  );
}
