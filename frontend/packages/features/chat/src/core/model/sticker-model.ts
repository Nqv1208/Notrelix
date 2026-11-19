/**
 * The Notrelix stickers (UC-CHT-I06). The server holds the catalog of ids (StickerCatalog); this is the art the client draws for each id.
 * An id the client does not know still shows its preview text, so a newer server never breaks an older client. Only these stickers exist:
 * a Workspace cannot upload one and there is no third-party GIF provider.
 */
export interface StickerArt {
  readonly id: string;
  /** The picture of the sticker. */
  readonly glyph: string;
}

/** In the order the picker shows them; the ids match the server catalog. */
export const STICKERS: readonly StickerArt[] = [
  { id: "thumbs-up", glyph: "👍" },
  { id: "clap", glyph: "👏" },
  { id: "party", glyph: "🎉" },
  { id: "heart", glyph: "❤️" },
  { id: "laugh", glyph: "😂" },
  { id: "wow", glyph: "😮" },
  { id: "thinking", glyph: "🤔" },
  { id: "sad", glyph: "😢" },
  { id: "fire", glyph: "🔥" },
  { id: "rocket", glyph: "🚀" },
  { id: "eyes", glyph: "👀" },
  { id: "check", glyph: "✅" },
  { id: "pray", glyph: "🙏" },
  { id: "hundred", glyph: "💯" },
  { id: "coffee", glyph: "☕" },
  { id: "bug", glyph: "🐛" },
];

const BY_ID = new Map(STICKERS.map((sticker) => [sticker.id, sticker]));

/** The art of a sticker id, or undefined for an id this client does not know. */
export function stickerArt(
  id: string | null | undefined,
): StickerArt | undefined {
  return id ? BY_ID.get(id) : undefined;
}
