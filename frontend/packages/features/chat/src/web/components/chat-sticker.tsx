import { cn } from "@notrelix/ui-web";
import { stickerArt } from "../../core/model/sticker-model";
import type { ChatCopy } from "./chat.copy";

/** The words of a sticker: its label in the viewer's language, or the preview text the server wrote for an id this client does not know. */
export function stickerLabel(
  copy: ChatCopy,
  stickerId: string,
  fallback: string | null,
): string {
  return copy.stickers.labels[stickerId] ?? fallback ?? stickerId;
}

/**
 * A Notrelix sticker (UC-CHT-I06): the art of the id on a quiet tile, with its label as the accessible name. An id this client has no
 * art for (a newer server) reads as its preview text, so nothing is lost.
 */
export function ChatSticker({
  stickerId,
  copy,
  previewText,
  size = "lg",
  highlighted = false,
}: {
  readonly stickerId: string;
  readonly copy: ChatCopy;
  readonly previewText?: string | null;
  readonly size?: "lg" | "sm";
  readonly highlighted?: boolean;
}) {
  const art = stickerArt(stickerId);
  const label = stickerLabel(copy, stickerId, previewText ?? null);
  if (!art) {
    return (
      <p
        className="px-3 py-2 text-sm italic text-muted-foreground"
        data-bubble="sticker"
        data-sticker-id={stickerId}
      >
        {label}
      </p>
    );
  }
  return (
    <span
      role="img"
      aria-label={label}
      data-bubble="sticker"
      data-sticker-id={stickerId}
      className={cn(
        "flex items-center justify-center rounded-2xl bg-muted select-none",
        size === "lg" ? "size-28 text-6xl" : "size-12 text-3xl",
        highlighted && "ring-2 ring-ring ring-offset-2 ring-offset-background",
      )}
    >
      <span aria-hidden="true">{art.glyph}</span>
    </span>
  );
}
