/**
 * The editor's keyboard shortcuts: one list read by the key bindings, the
 * menu bar (shown beside each item and as `aria-keyshortcuts`) and the
 * shortcut sheet. `Mod` is Command on macOS and Control elsewhere.
 */
export type ShortcutGroup = "text" | "blocks" | "edit" | "page";

export type ShortcutId =
  | "bold"
  | "italic"
  | "underline"
  | "strike"
  | "code"
  | "superscript"
  | "subscript"
  | "link"
  | "clearFormat"
  | "alignLeft"
  | "alignCenter"
  | "alignRight"
  | "alignJustify"
  | "paragraph"
  | "heading1"
  | "heading2"
  | "heading3"
  | "bulleted"
  | "numbered"
  | "checklist"
  | "quote"
  | "codeBlock"
  | "indent"
  | "outdent"
  | "moveUp"
  | "moveDown"
  | "undo"
  | "redo"
  | "selectAll"
  | "pastePlain"
  | "find"
  | "replace"
  | "comment"
  | "wordCount"
  | "shortcuts"
  | "focusMode"
  | "print";

export interface ShortcutDef {
  readonly id: ShortcutId;
  readonly keys: string;
  readonly group: ShortcutGroup;
}

export const SHORTCUTS: readonly ShortcutDef[] = [
  { id: "bold", keys: "Mod-b", group: "text" },
  { id: "italic", keys: "Mod-i", group: "text" },
  { id: "underline", keys: "Mod-u", group: "text" },
  { id: "strike", keys: "Mod-Shift-s", group: "text" },
  { id: "code", keys: "Mod-e", group: "text" },
  { id: "superscript", keys: "Mod-.", group: "text" },
  { id: "subscript", keys: "Mod-,", group: "text" },
  { id: "link", keys: "Mod-k", group: "text" },
  { id: "clearFormat", keys: "Mod-\\", group: "text" },
  { id: "alignLeft", keys: "Mod-Shift-l", group: "text" },
  { id: "alignCenter", keys: "Mod-Shift-e", group: "text" },
  { id: "alignRight", keys: "Mod-Shift-r", group: "text" },
  { id: "alignJustify", keys: "Mod-Shift-j", group: "text" },
  { id: "paragraph", keys: "Mod-Alt-0", group: "blocks" },
  { id: "heading1", keys: "Mod-Alt-1", group: "blocks" },
  { id: "heading2", keys: "Mod-Alt-2", group: "blocks" },
  { id: "heading3", keys: "Mod-Alt-3", group: "blocks" },
  { id: "numbered", keys: "Mod-Shift-7", group: "blocks" },
  { id: "bulleted", keys: "Mod-Shift-8", group: "blocks" },
  { id: "checklist", keys: "Mod-Shift-9", group: "blocks" },
  { id: "quote", keys: "Mod-Shift-b", group: "blocks" },
  { id: "codeBlock", keys: "Mod-Alt-c", group: "blocks" },
  { id: "indent", keys: "Mod-]", group: "blocks" },
  { id: "outdent", keys: "Mod-[", group: "blocks" },
  { id: "moveUp", keys: "Alt-Shift-ArrowUp", group: "blocks" },
  { id: "moveDown", keys: "Alt-Shift-ArrowDown", group: "blocks" },
  { id: "undo", keys: "Mod-z", group: "edit" },
  { id: "redo", keys: "Mod-Shift-z", group: "edit" },
  { id: "selectAll", keys: "Mod-a", group: "edit" },
  { id: "pastePlain", keys: "Mod-Shift-v", group: "edit" },
  { id: "find", keys: "Mod-f", group: "edit" },
  { id: "replace", keys: "Mod-h", group: "edit" },
  { id: "comment", keys: "Mod-Alt-m", group: "page" },
  { id: "wordCount", keys: "Mod-Shift-c", group: "page" },
  { id: "focusMode", keys: "Mod-Shift-f", group: "page" },
  { id: "shortcuts", keys: "Mod-/", group: "page" },
  { id: "print", keys: "Mod-p", group: "page" },
];

export function shortcutKeys(id: ShortcutId): string {
  const found = SHORTCUTS.find((entry) => entry.id === id);
  return found ? found.keys : "";
}

export function isMacPlatform(): boolean {
  if (typeof navigator === "undefined") return false;
  const platform =
    (navigator as { userAgentData?: { platform?: string } }).userAgentData
      ?.platform ?? navigator.platform;
  return /mac|iphone|ipad/i.test(platform ?? "");
}

const MAC_GLYPH: Readonly<Record<string, string>> = {
  Mod: "⌘",
  Shift: "⇧",
  Alt: "⌥",
  Ctrl: "⌃",
  ArrowUp: "↑",
  ArrowDown: "↓",
  ArrowLeft: "←",
  ArrowRight: "→",
};

const PLAIN_NAME: Readonly<Record<string, string>> = {
  Mod: "Ctrl",
  ArrowUp: "↑",
  ArrowDown: "↓",
  ArrowLeft: "←",
  ArrowRight: "→",
};

/** "Mod-Shift-z" as the member reads it: "⌘⇧Z" on a Mac, "Ctrl+Shift+Z" elsewhere. */
export function shortcutLabel(keys: string, mac = isMacPlatform()): string {
  if (!keys) return "";
  const parts = keys.split("-").map((part) => (part === "" ? "-" : part));
  if (mac) {
    return parts.map((part) => MAC_GLYPH[part] ?? part.toUpperCase()).join("");
  }
  return parts
    .map((part) =>
      part.length === 1 ? part.toUpperCase() : (PLAIN_NAME[part] ?? part),
    )
    .join("+");
}

/** The value of `aria-keyshortcuts` ("Meta+Shift+Z" / "Control+Shift+Z"). */
export function ariaShortcut(keys: string, mac = isMacPlatform()): string {
  if (!keys) return "";
  return keys
    .split("-")
    .map((part) => {
      if (part === "Mod") return mac ? "Meta" : "Control";
      return part.length === 1 ? part.toUpperCase() : part;
    })
    .join("+");
}
