/**
 * Rules of message attachments (UC-CHT-G08), framework-neutral: the client-side pre-check, the reasons the
 * server gives for a refused file, the glyph of a type and the shape of one file's upload.
 */

import { CHAT_LIMITS, type ChatAttachment } from "../types/chat";

/** What the web shows inline as a preview; everything else is a file card (the server serves only these inline). */
const INLINE_IMAGE_TYPES: ReadonlySet<string> = new Set([
  "image/png",
  "image/jpeg",
  "image/gif",
  "image/webp",
]);

export function isInlineImage(contentType: string): boolean {
  return INLINE_IMAGE_TYPES.has(contentType.toLowerCase());
}

export type AttachmentGlyph =
  | "image"
  | "video"
  | "audio"
  | "pdf"
  | "archive"
  | "sheet"
  | "document"
  | "text"
  | "file";

const EXTENSION_GLYPHS: Readonly<Record<string, AttachmentGlyph>> = {
  pdf: "pdf",
  zip: "archive",
  gz: "archive",
  tar: "archive",
  "7z": "archive",
  rar: "archive",
  xls: "sheet",
  xlsx: "sheet",
  csv: "sheet",
  ods: "sheet",
  doc: "document",
  docx: "document",
  odt: "document",
  rtf: "document",
  ppt: "document",
  pptx: "document",
  txt: "text",
  md: "text",
  json: "text",
  log: "text",
};

/** The icon family of a file: the content type first, then the extension. */
export function glyphOf(
  contentType: string,
  fileName: string,
): AttachmentGlyph {
  const type = contentType.toLowerCase();
  if (type.startsWith("image/")) return "image";
  if (type.startsWith("video/")) return "video";
  if (type.startsWith("audio/")) return "audio";
  if (type === "application/pdf") return "pdf";
  const dot = fileName.lastIndexOf(".");
  const extension = dot >= 0 ? fileName.slice(dot + 1).toLowerCase() : "";
  const byExtension = EXTENSION_GLYPHS[extension];
  if (byExtension) return byExtension;
  if (type.startsWith("text/")) return "text";
  return "file";
}

/** "0 B", "950 B", "1.5 KB", "12 MB": binary steps, one decimal under 10. */
export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) return "0 B";
  const units = ["B", "KB", "MB", "GB"] as const;
  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  const text =
    unit === 0 || value >= 10
      ? String(Math.round(value))
      : (Math.round(value * 10) / 10).toString();
  return `${text} ${units[unit]}`;
}

/** Why the pre-check leaves a file out of the pending list. */
export type PendingFileRefusal = "too-many" | "too-large" | "empty";

export interface PendingFileCheck {
  readonly accepted: readonly File[];
  readonly refused: readonly {
    readonly name: string;
    readonly reason: PendingFileRefusal;
  }[];
}

/**
 * The client-side pre-check of picked, dropped or pasted files: a message holds at most 10 files, a file is
 * not empty and not over 100 MB. The type is not checked here: the server owns the blocked-type policy and
 * answers 415 for that file, which the message then shows with its reason.
 */
export function checkPendingFiles(
  held: number,
  incoming: readonly File[],
): PendingFileCheck {
  const accepted: File[] = [];
  const refused: { name: string; reason: PendingFileRefusal }[] = [];
  for (const file of incoming) {
    if (file.size === 0) {
      refused.push({ name: file.name, reason: "empty" });
    } else if (file.size > CHAT_LIMITS.attachmentMaxBytes) {
      refused.push({ name: file.name, reason: "too-large" });
    } else if (held + accepted.length >= CHAT_LIMITS.attachmentsPerMessageMax) {
      refused.push({ name: file.name, reason: "too-many" });
    } else {
      accepted.push(file);
    }
  }
  return { accepted, refused };
}

/** Why the server refused one file (or could not take it). */
export type UploadFailureReason =
  /** 413 */
  | "too-large"
  /** 415: a blocked or executable type */
  | "blocked"
  /** 409 attachment.limit_exceeded */
  | "limit"
  /** 400: for example an empty file */
  | "invalid"
  /** 409 (archived, deleted or system message), 404 */
  | "unavailable"
  /** 403 */
  | "forbidden"
  /** no response, 429 or 5xx: a retry can work */
  | "retryable";

export function uploadFailureOf(error: unknown): UploadFailureReason {
  const like =
    error && typeof error === "object"
      ? (error as { status?: unknown; code?: unknown })
      : {};
  const status = typeof like.status === "number" ? like.status : undefined;
  const code = typeof like.code === "string" ? like.code : undefined;
  if (code === "attachment.limit_exceeded") return "limit";
  if (status === 413) return "too-large";
  if (status === 415) return "blocked";
  if (status === 403) return "forbidden";
  if (status === 404) return "unavailable";
  if (status === 409) return "unavailable";
  if (status === 400 || status === 422) return "invalid";
  return "retryable";
}

/** Only a transient failure is worth the same file again; a refusal stays refused. */
export function isRetryableUpload(reason: UploadFailureReason): boolean {
  return reason === "retryable";
}

export type ChatUploadStatus = "queued" | "uploading" | "failed";

/** One file on its way to a message that is already stored (client-only; the message holds the file once stored). */
export interface ChatUploadItem {
  readonly id: string;
  readonly messageId: string;
  readonly spaceId: string;
  readonly file: File;
  readonly status: ChatUploadStatus;
  /** 0 to 1 while uploading (a transport without progress stays 0 until the file is stored). */
  readonly progress: number;
  readonly reason: UploadFailureReason | null;
  /** One per file: the same key replays the stored file when a retry follows an answer that was lost. */
  readonly idempotencyKey: string;
}

/** The files of a message, once per id, oldest first. */
export function mergeAttachment(
  current: readonly ChatAttachment[] | undefined,
  attachment: ChatAttachment,
): readonly ChatAttachment[] {
  const held = current ?? [];
  return held.some((item) => item.id === attachment.id)
    ? held
    : [...held, attachment];
}

/** The narrowest and widest shape a reserved image box takes (3:4 to 2:1); a wider or taller image is cropped to it. */
export const IMAGE_BOX_MIN_RATIO = 0.75;
export const IMAGE_BOX_MAX_RATIO = 2;

/**
 * The width-to-height ratio of an image's reserved box, from its pixel size and clamped to 3:4..2:1, or null when the
 * size is not known (the layout keeps its square or wide fallback).
 */
export function imageBoxRatio(
  attachment: Pick<ChatAttachment, "width" | "height">,
): number | null {
  const { width, height } = attachment;
  if (typeof width !== "number" || typeof height !== "number") return null;
  if (!(width >= 1 && height >= 1)) return null;
  return Math.min(
    IMAGE_BOX_MAX_RATIO,
    Math.max(IMAGE_BOX_MIN_RATIO, width / height),
  );
}
