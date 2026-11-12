/**
 * How one message is displayed (the Messenger-style list): framework-neutral rules the web row reads.
 */

/** At most this many emoji, with nothing else, show large and without a bubble. */
export const EMOJI_ONLY_MAX = 3;

const PICTOGRAPHIC = /\p{Extended_Pictographic}/u;
/** Joiners, variation selectors, skin tones and keycap marks that belong to an emoji. */
const EMOJI_PARTS =
  /^(?:\p{Extended_Pictographic}|\p{Emoji_Modifier}|\p{Regional_Indicator}|[‍︎️⃣#*0-9])+$/u;

function graphemes(text: string): string[] {
  const Segmenter = (
    Intl as unknown as {
      Segmenter?: new (
        locale?: string,
        options?: { granularity: "grapheme" },
      ) => { segment(input: string): Iterable<{ segment: string }> };
    }
  ).Segmenter;
  if (Segmenter) {
    return Array.from(
      new Segmenter(undefined, { granularity: "grapheme" }).segment(text),
      (part) => part.segment,
    );
  }
  return Array.from(text);
}

/**
 * The number of emoji when the text is one to three emoji and nothing else (spaces allowed), else 0.
 * A digit or # alone is text, not an emoji.
 */
export function emojiOnlyCount(text: string | null | undefined): number {
  if (!text) return 0;
  const compact = text.replace(/\s+/gu, "");
  if (compact.length === 0 || compact.length > 40) return 0;
  const parts = graphemes(compact);
  if (parts.length === 0 || parts.length > EMOJI_ONLY_MAX) return 0;
  for (const part of parts) {
    if (!EMOJI_PARTS.test(part)) return 0;
    // A keycap needs its mark; a lone digit, # or * is text.
    if (!PICTOGRAPHIC.test(part) && !/⃣|\p{Regional_Indicator}/u.test(part))
      return 0;
  }
  return parts.length;
}

/** What the space lets the viewer do with messages, as the list offers it (UC-CHT-H01..H04). */
export interface MessageActionScope {
  /** The channel is archived: it is read-only for everyone. */
  readonly archived: boolean;
  /** Why posting is refused (archived, not a member, read-only role, blocked); null when the viewer may post. */
  readonly postRefused: boolean;
  /** The server's capabilities; absent from older servers, which keep today's behaviour (allowed). */
  readonly canReact?: boolean;
  readonly canPin?: boolean;
  readonly canModerate?: boolean;
}

/** Which message actions the list offers in this space. Edit and delete stay with the author's own live messages. */
export interface MessageActionGrants {
  readonly react: boolean;
  readonly reply: boolean;
  readonly pin: boolean;
  readonly editOwn: boolean;
  readonly deleteOwn: boolean;
  readonly moderate: boolean;
}

export function messageActionGrants(
  scope: MessageActionScope,
): MessageActionGrants {
  const writable = !scope.archived;
  return {
    react: writable && scope.canReact !== false,
    reply: writable && !scope.postRefused,
    pin: writable && scope.canPin !== false,
    editOwn: writable,
    deleteOwn: writable,
    // Moderation keeps the server's word, archived or not (it was offered that way before).
    moderate: scope.canModerate === true,
  };
}
