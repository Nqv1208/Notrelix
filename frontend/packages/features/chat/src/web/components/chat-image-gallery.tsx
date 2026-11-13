import { useEffect, useState, type KeyboardEvent } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Download,
  Image as ImageIcon,
} from "lucide-react";
import {
  Button,
  cn,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import type { ChatAttachment } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

/** The most tiles a gallery draws; a fifth image and beyond show as +N on the last tile. */
export const GALLERY_MAX_TILES = 4;

/**
 * The bytes of an attachment, read with the session into an object URL (a plain address would carry no
 * credentials). The URL is revoked when the bytes change or the component goes away.
 */
export function useAttachmentImageUrl(
  hooks: ChatHooks,
  workspaceId: string,
  messageId: string,
  attachmentId: string,
) {
  const content = hooks.useAttachmentContent(
    workspaceId,
    messageId,
    attachmentId,
  );
  const [url, setUrl] = useState<string | null>(null);
  const blob = content.data;
  useEffect(() => {
    if (!blob) {
      setUrl(null);
      return;
    }
    const next = URL.createObjectURL(blob);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [blob]);
  return { url, isError: content.isError };
}

interface GalleryShared {
  readonly messageId: string;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  readonly copy: ChatCopy;
}

/** The tile layout of a gallery: how many tiles, and which one spans the whole width. */
export function galleryLayout(count: number) {
  const tiles = Math.min(count, GALLERY_MAX_TILES);
  return {
    tiles,
    hidden: Math.max(0, count - tiles),
    // Three images: the first one is wide, the other two share the row under it.
    wideFirst: tiles === 3,
  };
}

/**
 * Two or more images of one message as one grid (UC-CHT-I01): at most four tiles, the last one saying how
 * many more there are. Every tile reserves its box before the image arrives, so the list does not jump.
 */
export function ImageGallery({
  images,
  onOpen,
  align = "start",
  ...shared
}: GalleryShared & {
  readonly images: readonly ChatAttachment[];
  readonly onOpen: (index: number) => void;
  readonly align?: "start" | "end";
}) {
  const layout = galleryLayout(images.length);
  return (
    <ul
      className={cn(
        "grid w-64 max-w-full grid-cols-2 gap-0.5 overflow-hidden rounded-2xl border border-border",
        align === "end" && "self-end",
      )}
      data-testid="chat-gallery"
      data-count={images.length}
    >
      {images.slice(0, layout.tiles).map((attachment, index) => {
        const isLast = index === layout.tiles - 1;
        return (
          <li
            key={attachment.id}
            className={cn(
              "relative",
              layout.wideFirst && index === 0
                ? "col-span-2 aspect-[2/1]"
                : "aspect-square",
            )}
          >
            <GalleryTile
              attachment={attachment}
              index={index}
              more={isLast ? layout.hidden : 0}
              onOpen={onOpen}
              {...shared}
            />
          </li>
        );
      })}
    </ul>
  );
}

function GalleryTile({
  attachment,
  index,
  more,
  onOpen,
  messageId,
  workspaceId,
  hooks,
  copy,
}: GalleryShared & {
  readonly attachment: ChatAttachment;
  readonly index: number;
  readonly more: number;
  readonly onOpen: (index: number) => void;
}) {
  const { url, isError } = useAttachmentImageUrl(
    hooks,
    workspaceId,
    messageId,
    attachment.id,
  );
  const label = more
    ? fillCopy(copy.attachments.viewer.more, { count: String(more) })
    : fillCopy(copy.attachments.viewer.open, { name: attachment.fileName });
  return (
    <button
      type="button"
      onClick={() => onOpen(index)}
      aria-label={label}
      className="relative block size-full overflow-hidden bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none focus-visible:ring-inset"
    >
      {url ? (
        <img
          src={url}
          alt={attachment.fileName}
          className="block size-full object-cover"
        />
      ) : (
        <span
          role="status"
          aria-label={copy.attachments.previewLoading}
          className={cn(
            "flex size-full items-center justify-center text-muted-foreground",
            !isError && "animate-pulse motion-reduce:animate-none",
          )}
        >
          <ImageIcon className="size-5" aria-hidden="true" />
        </span>
      )}
      {more > 0 ? (
        <span
          aria-hidden="true"
          className="absolute inset-0 grid place-items-center bg-foreground/60 text-lg font-semibold tabular-nums text-background"
        >
          +{more}
        </span>
      ) : null}
    </button>
  );
}

/**
 * The full-size viewer (UC-CHT-I03): the images of one message one at a time, with previous and next (also
 * the arrow keys), the position, a download and a close. Escape and the backdrop close it.
 */
export function ImageViewer({
  images,
  index,
  onIndexChange,
  onClose,
  onDownload,
  messageId,
  workspaceId,
  hooks,
  copy,
}: GalleryShared & {
  readonly images: readonly ChatAttachment[];
  /** The shown image, or null while closed. */
  readonly index: number | null;
  readonly onIndexChange: (index: number) => void;
  readonly onClose: () => void;
  readonly onDownload: (attachment: ChatAttachment) => void;
}) {
  const open = index !== null && images[index] !== undefined;
  const current = open && index !== null ? images[index] : undefined;
  const last = images.length - 1;

  function go(next: number) {
    if (next < 0 || next > last) return;
    onIndexChange(next);
  }

  function onKeyDown(event: KeyboardEvent) {
    if (index === null) return;
    if (event.key === "ArrowLeft") {
      event.preventDefault();
      go(index - 1);
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      go(index + 1);
    }
  }

  const counter =
    index === null
      ? ""
      : fillCopy(copy.attachments.viewer.counter, {
          n: String(index + 1),
          total: String(images.length),
        });

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? null : onClose())}>
      <DialogContent
        className="w-[min(96vw,64rem)] max-w-none gap-3 sm:max-w-none"
        onKeyDown={onKeyDown}
        aria-describedby="chat-viewer-position"
        data-testid="chat-image-viewer"
      >
        <DialogHeader className="pe-10">
          <DialogTitle className="truncate">
            {current?.fileName ?? copy.attachments.viewer.label}
          </DialogTitle>
          <DialogDescription
            id="chat-viewer-position"
            className="tabular-nums"
            aria-live="polite"
          >
            {counter}
          </DialogDescription>
        </DialogHeader>
        {current ? (
          <ViewerImage
            key={current.id}
            attachment={current}
            messageId={messageId}
            workspaceId={workspaceId}
            hooks={hooks}
            copy={copy}
          />
        ) : null}
        <div className="flex items-center justify-between gap-2">
          <Button
            type="button"
            variant="outline"
            size="icon"
            aria-label={copy.attachments.viewer.previous}
            disabled={index === null || index <= 0}
            onClick={() => index !== null && go(index - 1)}
          >
            <ChevronLeft aria-hidden="true" />
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => current && onDownload(current)}
            disabled={!current}
          >
            <Download aria-hidden="true" />
            {copy.attachments.downloadShort}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon"
            aria-label={copy.attachments.viewer.next}
            disabled={index === null || index >= last}
            onClick={() => index !== null && go(index + 1)}
          >
            <ChevronRight aria-hidden="true" />
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function ViewerImage({
  attachment,
  messageId,
  workspaceId,
  hooks,
  copy,
}: GalleryShared & { readonly attachment: ChatAttachment }) {
  const { url, isError } = useAttachmentImageUrl(
    hooks,
    workspaceId,
    messageId,
    attachment.id,
  );
  if (url) {
    return (
      <img
        src={url}
        alt={attachment.fileName}
        className="mx-auto block max-h-[70vh] max-w-full rounded-lg object-contain"
      />
    );
  }
  return (
    <div
      role="status"
      aria-label={copy.attachments.previewLoading}
      className={cn(
        "mx-auto flex aspect-[4/3] w-full items-center justify-center rounded-lg bg-muted text-muted-foreground",
        !isError && "animate-pulse motion-reduce:animate-none",
      )}
    >
      <ImageIcon className="size-6" aria-hidden="true" />
    </div>
  );
}
