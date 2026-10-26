import type { Editor } from "@tiptap/react";

export type ClipboardAction = "cut" | "copy" | "paste" | "pastePlain";

/**
 * The Edit menu's Cut, Copy and Paste. Cut and copy ask the browser to act on
 * the selection; paste reads the clipboard (the browser may ask permission or
 * refuse, then `onBlocked` tells the member to use the keyboard shortcut).
 * Lives outside the pure surfaces: it touches the clipboard.
 */
export async function runClipboardAction(
  editor: Editor,
  action: ClipboardAction,
  onBlocked: () => void,
): Promise<void> {
  editor.view.focus();
  if (action === "cut" || action === "copy") {
    try {
      document.execCommand(action);
    } catch {
      onBlocked();
    }
    return;
  }
  try {
    if (action === "paste" && typeof navigator.clipboard?.read === "function") {
      const items = await navigator.clipboard.read();
      for (const item of items) {
        if (item.types.includes("text/html")) {
          const html = await (await item.getType("text/html")).text();
          editor.view.pasteHTML(html);
          return;
        }
      }
    }
    editor.view.pasteText(await navigator.clipboard.readText());
  } catch {
    onBlocked();
  }
}
