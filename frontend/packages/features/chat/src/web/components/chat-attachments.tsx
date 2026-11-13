import { useEffect, useState } from "react";
import {
  Download,
  File as FileIcon,
  FileArchive,
  FileSpreadsheet,
  FileText,
  Film,
  Image as ImageIcon,
  Music,
  RotateCw,
  Trash2,
  X,
} from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  formatFileSize,
  glyphOf,
  imageBoxRatio,
  isInlineImage,
  isRetryableUpload,
  type AttachmentGlyph,
  type ChatUploadItem,
  type UploadFailureReason,
} from "../../core/model/attachment-model";
import {
  CHAT_LIMITS,
  type ChatAttachment,
  type ChatMessage,
} from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { ConfirmDialog } from "./chat-dialogs";
import {
  ImageGallery,
  ImageViewer,
  singleImageBoxStyle,
} from "./chat-image-gallery";
import { fillCopy } from "./use-chat-copy";

const GLYPHS: Readonly<Record<AttachmentGlyph, typeof FileIcon>> = {
  image: ImageIcon,
  video: Film,
  audio: Music,
  pdf: FileText,
  archive: FileArchive,
  sheet: FileSpreadsheet,
  document: FileText,
  text: FileText,
  file: FileIcon,
};

/** The icon of a file by its type (decorative: the name carries the meaning). */
export function AttachmentGlyphIcon({
  contentType,
  fileName,
  className,
}: {
  readonly contentType: string;
  readonly fileName: string;
  readonly className?: string;
}) {
  const Icon = GLYPHS[glyphOf(contentType, fileName)];
  return (
    <Icon className={cn("size-4 shrink-0", className)} aria-hidden="true" />
  );
}

/** Hands a downloaded Blob to the browser as a saved file (same approach as the other features). */
export function saveBlobAs(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  anchor.style.display = "none";
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

export function uploadFailureText(
  copy: ChatCopy,
  reason: UploadFailureReason,
  name: string,
): string {
  const text = copy.attachments.failure;
  switch (reason) {
    case "too-large":
      return fillCopy(text.tooLarge, {
        name,
        max: formatFileSize(CHAT_LIMITS.attachmentMaxBytes),
      });
    case "blocked":
      return fillCopy(text.blocked, { name });
    case "limit":
      return fillCopy(text.limit, {
        name,
        max: CHAT_LIMITS.attachmentsPerMessageMax,
      });
    case "invalid":
      return fillCopy(text.invalid, { name });
    case "unavailable":
      return fillCopy(text.unavailable, { name });
    case "forbidden":
      return fillCopy(text.forbidden, { name });
    default:
      return fillCopy(text.retryable, { name });
  }
}

export interface MessageAttachmentsProps {
  readonly message: ChatMessage;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  /** Only the author removes a file or retries an upload. */
  readonly isOwn: boolean;
  /** An archived channel is read-only: no remove. */
  readonly readOnly?: boolean;
  readonly copy: ChatCopy;
  /** The side of the column the files hug: the end under the caller's own bubble. */
  readonly align?: "start" | "end";
}

/**
 * The files of a message (UC-CHT-G08): image previews for png, jpeg, gif and webp, a file card for the rest,
 * the files still uploading or failed (author only), download through the session and the author's remove
 * with a confirmation. A deleted message (tombstone) has none.
 */
export function MessageAttachments({
  message,
  workspaceId,
  hooks,
  isOwn,
  readOnly = false,
  copy,
  align = "start",
}: MessageAttachmentsProps) {
  const uploadsQuery = hooks.useUploads(workspaceId);
  const actions = hooks.useAttachmentActions(workspaceId);
  const downloader = hooks.useDownloadAttachment(workspaceId);
  const [removing, setRemoving] = useState<ChatAttachment | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [viewing, setViewing] = useState<number | null>(null);

  if (message.deleted) return null;
  const attachments = message.attachments ?? [];
  const uploads = isOwn
    ? (uploadsQuery.data ?? []).filter((item) => item.messageId === message.id)
    : [];
  // The notice of a removal outlives the last file, so the author hears that it went.
  if (attachments.length === 0 && uploads.length === 0 && !notice) return null;

  const canRemove = isOwn && !readOnly;
  const images = attachments.filter((item) => isInlineImage(item.contentType));
  const files = attachments.filter((item) => !isInlineImage(item.contentType));

  function download(attachment: ChatAttachment) {
    setNotice(null);
    downloader.mutate(
      { messageId: message.id, attachmentId: attachment.id },
      {
        onSuccess: (blob) => saveBlobAs(blob, attachment.fileName),
        onError: () =>
          setNotice(
            fillCopy(copy.attachments.downloadFailed, {
              name: attachment.fileName,
            }),
          ),
      },
    );
  }

  function confirmRemove() {
    const target = removing;
    if (!target) return;
    actions.remove.mutate(
      { messageId: message.id, attachmentId: target.id },
      {
        onSuccess: () => {
          setRemoving(null);
          setNotice(
            fillCopy(copy.attachments.removed, { name: target.fileName }),
          );
        },
        onError: () => {
          setRemoving(null);
          setNotice(
            fillCopy(copy.attachments.removeFailed, { name: target.fileName }),
          );
        },
      },
    );
  }

  return (
    <div
      className={cn(
        "mt-1 flex max-w-full flex-col gap-1",
        align === "end" ? "items-end" : "items-start",
      )}
      data-align={align}
      role="group"
      aria-label={copy.attachments.listLabel}
      data-testid="chat-attachments"
    >
      {images.length === 1 && images[0] ? (
        <ImagePreview
          attachment={images[0]}
          messageId={message.id}
          workspaceId={workspaceId}
          hooks={hooks}
          copy={copy}
          canRemove={canRemove}
          onOpen={() => setViewing(0)}
          onDownload={download}
          onRemove={setRemoving}
        />
      ) : images.length > 1 ? (
        <ImageGallery
          images={images}
          onOpen={setViewing}
          align={align}
          messageId={message.id}
          workspaceId={workspaceId}
          hooks={hooks}
          copy={copy}
        />
      ) : null}
      {files.length > 0 ? (
        <ul
          className={cn(
            "flex max-w-full flex-col gap-1",
            align === "end" ? "items-end" : "items-start",
          )}
        >
          {files.map((attachment) => (
            <li key={attachment.id} className="min-w-0 max-w-full">
              <FileCard
                attachment={attachment}
                copy={copy}
                canRemove={canRemove}
                onDownload={download}
                onRemove={setRemoving}
              />
            </li>
          ))}
        </ul>
      ) : null}
      {uploads.length > 0 ? (
        <ul className="flex flex-col gap-1.5">
          {uploads.map((item) => (
            <li key={item.id}>
              <UploadRow
                item={item}
                copy={copy}
                onRetry={() => actions.retry(item.id)}
                onDismiss={() => actions.dismiss(item.id)}
              />
            </li>
          ))}
        </ul>
      ) : null}
      {notice ? (
        <p role="status" className="text-xs text-muted-foreground">
          {notice}
        </p>
      ) : null}
      {images.length > 0 ? (
        <ImageViewer
          images={images}
          index={viewing}
          onIndexChange={setViewing}
          onClose={() => setViewing(null)}
          onDownload={download}
          messageId={message.id}
          workspaceId={workspaceId}
          hooks={hooks}
          copy={copy}
        />
      ) : null}
      <ConfirmDialog
        open={removing !== null}
        title={copy.attachments.removeTitle}
        body={fillCopy(copy.attachments.removeBody, {
          name: removing?.fileName ?? "",
        })}
        confirmLabel={copy.attachments.removeConfirm}
        cancelLabel={copy.list.cancel}
        destructive
        busy={actions.remove.isPending}
        onOpenChange={(open) => {
          if (!open) setRemoving(null);
        }}
        onConfirm={confirmRemove}
      />
    </div>
  );
}

function FileCard({
  attachment,
  copy,
  canRemove,
  onDownload,
  onRemove,
}: {
  readonly attachment: ChatAttachment;
  readonly copy: ChatCopy;
  readonly canRemove: boolean;
  readonly onDownload: (attachment: ChatAttachment) => void;
  readonly onRemove: (attachment: ChatAttachment) => void;
}) {
  return (
    <div className="flex max-w-full items-center gap-2.5 rounded-2xl border border-border bg-card py-2 ps-2 pe-1 text-sm shadow-xs">
      <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-muted text-muted-foreground">
        <AttachmentGlyphIcon
          contentType={attachment.contentType}
          fileName={attachment.fileName}
        />
      </span>
      <button
        type="button"
        onClick={() => onDownload(attachment)}
        aria-label={fillCopy(copy.attachments.download, {
          name: attachment.fileName,
        })}
        title={fillCopy(copy.attachments.download, {
          name: attachment.fileName,
        })}
        className="flex min-w-0 flex-col items-start rounded-sm text-start focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
      >
        <span className="max-w-56 truncate font-medium text-foreground">
          {attachment.fileName}
        </span>
        <span className="text-xs tabular-nums text-muted-foreground">
          {formatFileSize(attachment.sizeBytes)}
        </span>
      </button>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={fillCopy(copy.attachments.download, {
          name: attachment.fileName,
        })}
        title={copy.attachments.downloadShort}
        onClick={() => onDownload(attachment)}
      >
        <Download aria-hidden="true" />
      </Button>
      {canRemove ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="text-destructive hover:text-destructive"
          aria-label={fillCopy(copy.attachments.removeFile, {
            name: attachment.fileName,
          })}
          title={copy.attachments.removeShort}
          onClick={() => onRemove(attachment)}
        >
          <Trash2 aria-hidden="true" />
        </Button>
      ) : null}
    </div>
  );
}

/**
 * An image the server serves inline. The bytes are read with the session into an object URL (a plain
 * address would carry no credentials); when they cannot be read the file shows as a card instead.
 */
function ImagePreview({
  attachment,
  messageId,
  workspaceId,
  hooks,
  copy,
  canRemove,
  onOpen,
  onDownload,
  onRemove,
}: {
  readonly attachment: ChatAttachment;
  readonly messageId: string;
  readonly workspaceId: string;
  readonly hooks: ChatHooks;
  readonly copy: ChatCopy;
  readonly canRemove: boolean;
  readonly onOpen: () => void;
  readonly onDownload: (attachment: ChatAttachment) => void;
  readonly onRemove: (attachment: ChatAttachment) => void;
}) {
  const content = hooks.useAttachmentContent(
    workspaceId,
    messageId,
    attachment.id,
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

  if (content.isError) {
    return (
      <FileCard
        attachment={attachment}
        copy={copy}
        canRemove={canRemove}
        onDownload={onDownload}
        onRemove={onRemove}
      />
    );
  }
  // A known pixel size reserves the preview box before the bytes arrive; an unknown one keeps the fallback sizes.
  const ratio = imageBoxRatio(attachment);
  const box = ratio === null ? null : singleImageBoxStyle(ratio);
  return (
    <figure className="group/image relative m-0 inline-flex max-w-full flex-col gap-1">
      {url ? (
        <button
          type="button"
          onClick={onOpen}
          aria-label={fillCopy(copy.attachments.viewer.open, {
            name: attachment.fileName,
          })}
          style={box ?? undefined}
          className="block max-w-full overflow-hidden rounded-2xl border border-border bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
        >
          <img
            src={url}
            alt={attachment.fileName}
            className={
              box
                ? "block size-full object-cover"
                : "block max-h-72 min-h-24 max-w-full min-w-24 object-cover sm:max-w-80"
            }
          />
        </button>
      ) : (
        <div
          role="status"
          aria-label={copy.attachments.previewLoading}
          style={box ?? undefined}
          className={cn(
            "flex max-w-full animate-pulse items-center justify-center rounded-2xl border border-border bg-muted text-muted-foreground motion-reduce:animate-none",
            !box && "aspect-[4/3] w-56",
          )}
        >
          <ImageIcon className="size-5" aria-hidden="true" />
        </div>
      )}
      <figcaption className="flex items-center gap-1 text-xs text-muted-foreground">
        <span className="min-w-0 max-w-56 truncate">{attachment.fileName}</span>
        <span className="tabular-nums">
          {formatFileSize(attachment.sizeBytes)}
        </span>
        {canRemove ? (
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="text-destructive hover:text-destructive"
            aria-label={fillCopy(copy.attachments.removeFile, {
              name: attachment.fileName,
            })}
            title={copy.attachments.removeShort}
            onClick={() => onRemove(attachment)}
          >
            <Trash2 aria-hidden="true" />
          </Button>
        ) : null}
      </figcaption>
    </figure>
  );
}

function UploadRow({
  item,
  copy,
  onRetry,
  onDismiss,
}: {
  readonly item: ChatUploadItem;
  readonly copy: ChatCopy;
  readonly onRetry: () => void;
  readonly onDismiss: () => void;
}) {
  const name = item.file.name;
  if (item.status === "failed") {
    const reason = item.reason ?? "retryable";
    return (
      <div
        role="alert"
        className="flex flex-wrap items-center gap-2 rounded-lg border border-destructive/40 bg-card px-2 py-1.5 text-xs"
        data-testid="chat-upload-failed"
      >
        <AttachmentGlyphIcon
          contentType={item.file.type}
          fileName={name}
          className="text-destructive"
        />
        <span className="min-w-0 flex-1 text-destructive">
          {uploadFailureText(copy, reason, name)}
        </span>
        {isRetryableUpload(reason) ? (
          <Button
            type="button"
            size="xs"
            variant="outline"
            aria-label={fillCopy(copy.attachments.retryFile, { name })}
            onClick={onRetry}
          >
            <RotateCw aria-hidden="true" />
            {copy.attachments.retry}
          </Button>
        ) : null}
        <Button
          type="button"
          size="xs"
          variant="ghost"
          aria-label={fillCopy(copy.attachments.dismissFile, { name })}
          onClick={onDismiss}
        >
          <X aria-hidden="true" />
          {copy.attachments.dismiss}
        </Button>
      </div>
    );
  }
  const percent = Math.round(item.progress * 100);
  // A transport without progress events (and a file not started) shows an indeterminate bar.
  const determinate = item.status === "uploading" && item.progress > 0;
  const label =
    item.status === "queued"
      ? fillCopy(copy.attachments.waiting, { name })
      : fillCopy(copy.attachments.uploading, { name });
  return (
    <div
      className="flex items-center gap-2 rounded-lg border border-border bg-card px-2 py-1.5 text-xs text-muted-foreground"
      data-testid="chat-upload-progress"
      data-status={item.status}
    >
      <AttachmentGlyphIcon contentType={item.file.type} fileName={name} />
      <span className="min-w-0 max-w-56 truncate">{label}</span>
      <span className="tabular-nums">{formatFileSize(item.file.size)}</span>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={determinate ? percent : undefined}
        className="h-1.5 w-24 shrink-0 overflow-hidden rounded-full bg-muted"
      >
        <div
          className={cn(
            "h-full bg-primary transition-[width]",
            !determinate && "w-full animate-pulse",
          )}
          style={determinate ? { width: `${percent}%` } : undefined}
        />
      </div>
    </div>
  );
}
