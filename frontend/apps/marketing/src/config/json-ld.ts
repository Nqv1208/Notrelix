// Safe to embed inside <script type="application/ld+json">.
const escapeChar = (c: string) =>
  `\\u${c.charCodeAt(0).toString(16).padStart(4, "0")}`;

export const serializeJsonLd = (data: unknown): string =>
  JSON.stringify(data)
    .replace(/[<>&]/g, escapeChar)
    .replace(
      new RegExp(`[${String.fromCharCode(0x2028, 0x2029)}]`, "g"),
      escapeChar,
    );
