import { useEffect, useRef } from "react";
import {
  createShortcutMatcher,
  type ShortcutBinding,
} from "../../core/model/shortcuts";

/** Shell shortcut ids (WX-SHELL-005). */
export type ShellShortcutId =
  "go-home" | "go-my-work" | "go-inbox" | "open-palette" | "show-shortcuts";

export const SHELL_SHORTCUTS: readonly (ShortcutBinding & {
  id: ShellShortcutId;
})[] = [
  { id: "go-home", keys: ["g", "h"] },
  { id: "go-my-work", keys: ["g", "m"] },
  { id: "go-inbox", keys: ["g", "i"] },
  { id: "open-palette", keys: ["k"], mod: true, whileTyping: true },
  { id: "show-shortcuts", keys: ["?"] },
];

/** True when focus is in a field where keys are text, not commands. */
export function isTypingTarget(target: EventTarget | null): boolean {
  if (!target || typeof (target as HTMLElement).tagName !== "string")
    return false;
  const element = target as HTMLElement;
  const tag = element.tagName.toLowerCase();
  if (tag === "input" || tag === "textarea" || tag === "select") return true;
  if (element.isContentEditable) return true;
  return (
    element.closest?.(
      "[contenteditable=''],[contenteditable='true'],[role='textbox']",
    ) != null
  );
}

/**
 * The one global keyboard registration of the workspace shell. Sequences
 * ("G" then "H") are skipped while typing; ⌘K / Ctrl+K works everywhere.
 */
export function useShellShortcuts(
  onShortcut: (id: ShellShortcutId) => void,
  options: { enabled?: boolean } = {},
): void {
  const handlerRef = useRef(onShortcut);
  handlerRef.current = onShortcut;
  const enabled = options.enabled ?? true;

  useEffect(() => {
    if (!enabled) return;
    const matcher = createShortcutMatcher(SHELL_SHORTCUTS);
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.repeat || event.isComposing) return;
      const id = matcher.handle({
        key: event.key,
        metaKey: event.metaKey,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        timeStamp: event.timeStamp,
        typing: isTypingTarget(event.target),
      }) as ShellShortcutId | null;
      if (!id) return;
      event.preventDefault();
      handlerRef.current(id);
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [enabled]);
}

/** Platform label of the palette chord. */
export function commandShortcutLabel(platform: string | undefined): string {
  return platform && /mac|iphone|ipad/i.test(platform) ? "⌘K" : "Ctrl K";
}
