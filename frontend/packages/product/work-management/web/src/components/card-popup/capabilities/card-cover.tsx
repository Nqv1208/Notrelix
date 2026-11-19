import { cn } from "@notrelix/ui-web";
import type {
  CardCover as CardCoverModel,
  CardCoverColor,
} from "@notrelix/work-management-core";
import { useWmUiPorts } from "../../shared/wm-ui-ports";

/** Literal classes, so Tailwind sees every one of them. */
export const COVER_COLOR_CLASS: Readonly<Record<CardCoverColor, string>> = {
  blue: "bg-cover-blue",
  teal: "bg-cover-teal",
  violet: "bg-cover-violet",
  amber: "bg-cover-amber",
  rose: "bg-cover-rose",
  gray: "bg-cover-gray",
  orange: "bg-cover-orange",
  green: "bg-cover-green",
  indigo: "bg-cover-indigo",
  red: "bg-cover-red",
};

/** The image of a cover attachment, read by the host (see `WmUiPorts.useCardImageUrl`). */
export function useCoverImageUrl(
  cardId: string,
  attachmentId: string | null | undefined,
): string | null {
  return useWmUiPorts().useCardImageUrl(cardId, attachmentId);
}

/**
 * The cover as a band: a colour or an image. Decorative (`aria-hidden`); the card's meaning never depends
 * on it. The caller places it: a strip sits at the top of the card or popup, a full cover fills the card.
 */
export function CardCover({
  cover,
  cardId,
  className,
}: {
  cover: CardCoverModel;
  cardId: string;
  className?: string;
}) {
  const imageUrl = useCoverImageUrl(cardId, cover.attachmentId);
  return (
    <span
      aria-hidden="true"
      data-card-cover={cover.style}
      className={cn(
        "block overflow-hidden bg-state-hover-strong",
        cover.color ? COVER_COLOR_CLASS[cover.color] : null,
        className,
      )}
    >
      {imageUrl ? (
        <img src={imageUrl} alt="" className="size-full object-cover" />
      ) : null}
    </span>
  );
}
