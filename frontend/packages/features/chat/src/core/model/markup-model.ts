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

/**
 * Block markup (UC-CHT-B08): a line that starts with "> " is a quote, "- " or "* " a bullet item, "1. " a numbered item. Runs of
 * the same kind group together; every other line stays paragraph text. A message with no such line is one paragraph, so
 * plain text is drawn exactly as before. Item and quote text still goes through tokenizeInline, so nothing becomes markup
 * the browser parses.
 */
export type BlockToken =
  | { readonly type: "paragraph"; readonly text: string }
  | { readonly type: "quote"; readonly text: string }
  | {
      readonly type: "list";
      readonly ordered: boolean;
      readonly items: readonly string[];
    };

const QUOTE = /^> ?(.*)$/;
const BULLET = /^[-*] +(\S.*)$/;
const NUMBERED = /^\d{1,3}\. +(\S.*)$/;

export function tokenizeBlocks(content: string): BlockToken[] {
  const blocks: BlockToken[] = [];
  let paragraph: string[] = [];
  const flush = () => {
    if (paragraph.length > 0) {
      blocks.push({ type: "paragraph", text: paragraph.join("\n") });
      paragraph = [];
    }
  };
  for (const line of content.split("\n")) {
    const quote = QUOTE.exec(line);
    const bullet = BULLET.exec(line);
    const numbered = NUMBERED.exec(line);
    const item = bullet ?? numbered;
    if (quote) {
      flush();
      const last = blocks[blocks.length - 1];
      if (last?.type === "quote") {
        blocks[blocks.length - 1] = {
          type: "quote",
          text: `${last.text}\n${quote[1]}`,
        };
      } else {
        blocks.push({ type: "quote", text: quote[1]! });
      }
    } else if (item) {
      flush();
      const last = blocks[blocks.length - 1];
      const ordered = numbered !== null && bullet === null;
      if (last?.type === "list" && last.ordered === ordered) {
        blocks[blocks.length - 1] = {
          type: "list",
          ordered,
          items: [...last.items, item[1]!],
        };
      } else {
        blocks.push({ type: "list", ordered, items: [item[1]!] });
      }
    } else {
      paragraph.push(line);
    }
  }
  flush();
  return blocks;
}
