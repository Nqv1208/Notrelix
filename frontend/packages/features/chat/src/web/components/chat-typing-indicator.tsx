import { Avatar, AvatarFallback, cn } from "@notrelix/ui-web";
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

/** The dots' stagger: each starts a little later, so the three pulse in a wave. */
const DOT_DELAYS = [
  "[animation-delay:0ms]",
  "[animation-delay:160ms]",
  "[animation-delay:320ms]",
];

/**
 * The typing bubble at the end of the list (Messenger): the first typer's avatar and a bubble of three dots
 * that pulse (still under reduced motion). Decorative: the live region above the composer says who is typing.
 */
export function ChatTypingBubble({
  ids,
  authorName,
  copy,
}: {
  readonly ids: readonly string[];
  readonly authorName: (userId: string) => string;
  readonly copy: ChatCopy;
}) {
  const first = ids[0];
  if (!first) return null;
  const names = ids.map((id) => authorName(id));
  return (
    <div
      className="mt-3 flex items-end gap-2 px-3 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-150 md:px-4"
      data-testid="chat-typing-bubble"
      title={typingText(names, copy)}
    >
      <span className="relative inline-flex w-7 shrink-0">
        <Avatar className="size-7">
          <AvatarFallback
            className={cn("text-xs font-semibold", avatarToneOf(first))}
          >
            {initialsOf(names[0] ?? "")}
          </AvatarFallback>
        </Avatar>
      </span>
      <span className="inline-flex h-9 items-center gap-1 rounded-2xl bg-sidebar-accent px-3.5">
        {DOT_DELAYS.map((delay) => (
          <span
            key={delay}
            className={cn(
              "size-1.5 rounded-full bg-muted-foreground motion-safe:animate-bounce motion-reduce:opacity-70",
              delay,
            )}
          />
        ))}
      </span>
    </div>
  );
}
