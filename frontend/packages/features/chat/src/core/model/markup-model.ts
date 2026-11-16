/**
 * The inline markup a message may carry (UC-CHT-B08, CHT-008): **bold**, _italic_, `code` and http(s) links. Anything else,
 * HTML included, stays text: the result is a list of tokens the UI draws as React elements, so no string is ever parsed as
 * markup by the browser. A link keeps only an address that parses as http or https.
 */
export type InlineToken =
  | { readonly type: "text"; readonly text: string }
  | { readonly type: "bold" | "italic" | "code"; readonly text: string }
  | { readonly type: "link"; readonly text: string; readonly href: string };

const INLINE =
  /(`[^`\n]+`)|(\*\*[^*\n]+\*\*)|((?<![\p{L}\p{N}_])_[^_\n]+_(?![\p{L}\p{N}_]))|(https?:\/\/[^\s<>]+)/gu;

/** Trailing punctuation is the sentence's, not the address's. */
const TRAILING = /[.,;:!?'")\]]+$/;

export function safeHttpUrl(candidate: string): string | null {
  try {
    const url = new URL(candidate);
    return url.protocol === "http:" || url.protocol === "https:"
      ? url.href
      : null;
  } catch {
    return null;
  }
}

export function tokenizeInline(content: string): InlineToken[] {
  const tokens: InlineToken[] = [];
  let last = 0;
  const push = (token: InlineToken) => {
    const previous = tokens[tokens.length - 1];
    if (token.type === "text" && previous?.type === "text") {
      tokens[tokens.length - 1] = {
        type: "text",
        text: previous.text + token.text,
      };
    } else if (token.type !== "text" || token.text.length > 0) {
      tokens.push(token);
    }
  };
  for (const match of content.matchAll(INLINE)) {
    const start = match.index ?? 0;
    push({ type: "text", text: content.slice(last, start) });
    const [whole, code, bold, italic] = match;
    last = start + whole.length;
    if (code) {
      push({ type: "code", text: code.slice(1, -1) });
    } else if (bold) {
      push({ type: "bold", text: bold.slice(2, -2) });
    } else if (italic) {
      push({ type: "italic", text: italic.slice(1, -1) });
    } else {
      const trimmed = whole.replace(TRAILING, "");
      const href = safeHttpUrl(trimmed);
      if (href === null) {
        push({ type: "text", text: whole });
      } else {
        push({ type: "link", text: trimmed, href });
        push({ type: "text", text: whole.slice(trimmed.length) });
      }
    }
  }
  push({ type: "text", text: content.slice(last) });
  return tokens;
}
