/**
 * What the page checks before sending a file to a page's attachments. The
 * server decides: these checks only spare a doomed upload (the limits mirror
 * the attachment upload contract, `AttachmentUploadOptions`).
 */

import { isSafeHref, normalizeRuns, type Mark, type Run } from "./rich-text";

/** A file stored for a page (an image or file block points at it by id). */
export interface PageAttachment {
  readonly id: string;
  readonly fileName: string;
  readonly sizeBytes: number;
  readonly contentType: string;
  readonly createdAt: string;
}

/** The most a single file can be: 100 MB. */
export const MAX_PAGE_FILE_BYTES = 100 * 1024 * 1024;

/** Extensions the API refuses as executable or active content. */
export const BLOCKED_PAGE_FILE_EXTENSIONS: readonly string[] = [
  "exe",
  "msi",
  "dll",
  "bat",
  "cmd",
  "com",
  "scr",
  "ps1",
  "vbs",
  "sh",
  "jar",
  "apk",
  "js",
  "mjs",
  "html",
  "htm",
];

/** The width range of an image block, in pixels. */
export const IMAGE_MIN_WIDTH = 80;
export const IMAGE_MAX_WIDTH = 2000;

/** The longest alternative text an image block keeps. */
export const IMAGE_MAX_ALT_LENGTH = 500;

export type PageFileRefusal = "empty" | "tooBig" | "blockedType";

/** The extension of a file name, lower case, without the dot. */
export function fileExtension(name: string): string {
  const dot = name.lastIndexOf(".");
  return dot < 0
    ? ""
    : name
        .slice(dot + 1)
        .trim()
        .toLowerCase();
}

/** Why a file should not be sent, or null when it may be. */
export function refusePageFile(file: {
  readonly name: string;
  readonly size: number;
}): PageFileRefusal | null {
  if (file.size === 0) return "empty";
  if (file.size > MAX_PAGE_FILE_BYTES) return "tooBig";
  return BLOCKED_PAGE_FILE_EXTENSIONS.includes(fileExtension(file.name))
    ? "blockedType"
    : null;
}

/** Image types the server stores as images and the browser can show. */
const IMAGE_TYPES: ReadonlySet<string> = new Set([
  "image/png",
  "image/jpeg",
  "image/gif",
  "image/webp",
]);

/** Whether a file becomes an image block (by its type, else its extension). */
export function isPageImage(file: {
  readonly name: string;
  readonly type: string;
}): boolean {
  if (file.type) return IMAGE_TYPES.has(file.type.toLowerCase());
  return ["png", "jpg", "jpeg", "gif", "webp"].includes(
    fileExtension(file.name),
  );
}

/** An image width kept inside the contract's range. */
export function clampImageWidth(width: number): number {
  return Math.min(
    IMAGE_MAX_WIDTH,
    Math.max(IMAGE_MIN_WIDTH, Math.round(width)),
  );
}

/** A file size as people read it ("1,2 MB"). */
export function formatFileSize(bytes: number, locale = "vi"): string {
  const units = ["B", "KB", "MB", "GB"];
  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  const digits = unit === 0 || value >= 100 ? 0 : 1;
  return `${new Intl.NumberFormat(locale, {
    maximumFractionDigits: digits,
  }).format(value)} ${units[unit]}`;
}

/** The marks an image caption keeps: the same v1 marks, without colour or mentions. */
export const CAPTION_MARK_TYPES = ["bold", "italic", "code", "link"] as const;

/**
 * A caption's runs as the page stores them: only bold, italic, code and safe
 * links, one line (line breaks become spaces), empty runs dropped.
 */
export function sanitizeCaptionRuns(runs: readonly Run[]): Run[] {
  return normalizeRuns(
    runs.map((run) => ({
      text: run.text.replace(/\s*\n\s*/g, " "),
      marks: run.marks.filter(
        (mark): mark is Mark =>
          (CAPTION_MARK_TYPES as readonly string[]).includes(mark.type) &&
          (mark.type !== "link" || isSafeHref(mark.href)),
      ),
    })),
  );
}
