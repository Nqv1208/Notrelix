/**
 * Keyboard shortcut matcher (WX-SHELL-005).
 *
 * Pure state machine: feed key events, get the matched binding id. Supports
 * two-key sequences ("g" then "h") and one modified chord (⌘K / Ctrl+K).
 * Events raised while the user is typing are ignored, except a chord that
 * explicitly allows it (the command palette opens from an input too).
 */

export interface ShortcutBinding {
  readonly id: string;
  /** Lowercase key sequence, e.g. ["g", "h"], or a single key ["?"]. */
  readonly keys: readonly string[];
  /** Requires ⌘ (macOS) or Ctrl (others). Only for single-key chords. */
  readonly mod?: boolean;
  /** Fire even when focus is in an editable field. */
  readonly whileTyping?: boolean;
}

export interface ShortcutKeyEvent {
  readonly key: string;
  readonly metaKey: boolean;
  readonly ctrlKey: boolean;
  readonly altKey: boolean;
  /** Milliseconds; any monotonic clock. */
  readonly timeStamp: number;
  /** Focus is in an input, textarea, select or contenteditable element. */
  readonly typing: boolean;
}

export interface ShortcutMatcher {
  handle(event: ShortcutKeyEvent): string | null;
  reset(): void;
}

export const SHORTCUT_SEQUENCE_TIMEOUT_MS = 1200;

export function createShortcutMatcher(
  bindings: readonly ShortcutBinding[],
  sequenceTimeoutMs = SHORTCUT_SEQUENCE_TIMEOUT_MS,
): ShortcutMatcher {
  let pending: string[] = [];
  let lastAt = 0;

  const reset = () => {
    pending = [];
    lastAt = 0;
  };

  return {
    reset,
    handle(event) {
      const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
      const mod = event.metaKey || event.ctrlKey;

      if (mod) {
        reset();
        if (event.altKey) return null;
        const chord = bindings.find(
          (binding) =>
            binding.mod &&
            binding.keys.length === 1 &&
            binding.keys[0] === key &&
            (!event.typing || binding.whileTyping),
        );
        return chord?.id ?? null;
      }

      if (event.typing || event.altKey) {
        reset();
        return null;
      }

      if (pending.length > 0 && event.timeStamp - lastAt > sequenceTimeoutMs) {
        reset();
      }

      const candidate = [...pending, key];
      const exact = bindings.find(
        (binding) =>
          !binding.mod &&
          binding.keys.length === candidate.length &&
          binding.keys.every((part, index) => part === candidate[index]),
      );
      if (exact) {
        reset();
        return exact.id;
      }

      const isPrefix = bindings.some(
        (binding) =>
          !binding.mod &&
          binding.keys.length > candidate.length &&
          candidate.every((part, index) => binding.keys[index] === part),
      );
      if (isPrefix) {
        pending = candidate;
        lastAt = event.timeStamp;
        return null;
      }

      reset();
      // The key may start a new sequence on its own.
      const restart = bindings.some(
        (binding) =>
          !binding.mod && binding.keys.length > 1 && binding.keys[0] === key,
      );
      if (restart) {
        pending = [key];
        lastAt = event.timeStamp;
      }
      return null;
    },
  };
}
