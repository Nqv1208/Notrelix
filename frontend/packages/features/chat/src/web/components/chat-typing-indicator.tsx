import type { ChatCopy } from "./chat.copy";
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
 * The typing row above the composer. A polite, atomic live region: it is
 * always in the page (so a screen reader tracks it) and keeps its height, so
 * the composer does not jump when it fills and empties.
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
      className="min-h-5 px-4 text-xs text-muted-foreground"
    >
      {text}
    </div>
  );
}
