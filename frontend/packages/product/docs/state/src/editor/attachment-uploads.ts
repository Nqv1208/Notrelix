import {
  isPageImage,
  refusePageFile,
  type PageAttachment,
  type PageFileRefusal,
} from "@notrelix/docs-core";
import type { DocsUploadOptions } from "../api/page.api";

/** Why a file did not make it onto the page (the server's answer). */
export type UploadFailure =
  /** 413: over the size limit. */
  | "serverTooLarge"
  /** 415: a type the server does not store. */
  | "unsupportedType"
  /** 409: the page holds the most files it can. */
  | "limitReached"
  | "forbidden"
  | "notFound"
  /** The network or the server broke the transfer: retryable. */
  | "failed";

/** Reasons a retry can change; the rest need another file or another page. */
const RETRYABLE: ReadonlySet<UploadFailure> = new Set([
  "failed",
  "limitReached",
]);

export function canRetryUpload(failure: UploadFailure | null): boolean {
  return failure !== null && RETRYABLE.has(failure);
}

/** Maps what the API answered (an `AppError`) to the reason shown to the member. */
export function classifyUploadFailure(error: unknown): UploadFailure {
  const { status, code } =
    typeof error === "object" && error !== null
      ? (error as { status?: unknown; code?: unknown })
      : {};
  if (status === 413 || code === "attachment.payload_too_large") {
    return "serverTooLarge";
  }
  if (status === 415 || code === "attachment.unsupported_media_type") {
    return "unsupportedType";
  }
  if (status === 409 && code === "attachment.limit_exceeded") {
    return "limitReached";
  }
  if (status === 403) return "forbidden";
  if (status === 404) return "notFound";
  return "failed";
}

function isAborted(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (error as { kind?: unknown }).kind === "aborted"
  );
}

/** A file on its way to the page, or one that did not make it. */
export interface PendingUpload {
  readonly id: string;
  readonly fileName: string;
  readonly size: number;
  /** An image becomes an image block, any other file a file block. */
  readonly kind: "image" | "file";
  readonly status: "uploading" | "failed";
  /** 0 to 100 while uploading; null when the transfer reports none. */
  readonly progress: number | null;
  readonly failure: UploadFailure | null;
  /** A local preview of an image while it uploads; never stored. */
  readonly previewUrl: string | null;
}

export type AddUploadResult =
  | { readonly ok: true; readonly id: string }
  | { readonly ok: false; readonly refusal: PageFileRefusal };

export interface AttachmentUploadsOptions {
  readonly upload: (
    file: File,
    options: DocsUploadOptions,
  ) => Promise<PageAttachment>;
  /** The file is stored: the editor swaps the placeholder for the block. */
  readonly onUploaded: (
    upload: PendingUpload,
    attachment: PageAttachment,
  ) => void;
  /** A local preview address for an image (the editor shows it while uploading). */
  readonly createPreviewUrl?: (file: File) => string | null;
  readonly revokePreviewUrl?: (url: string) => void;
  readonly newId?: () => string;
}

interface Entry {
  upload: PendingUpload;
  readonly file: File;
  /** One per user action; reused only to retry that same action. */
  readonly idempotencyKey: string;
  controller: AbortController | null;
}

function browserPreview(file: File): string | null {
  return typeof URL !== "undefined" && typeof URL.createObjectURL === "function"
    ? URL.createObjectURL(file)
    : null;
}

function browserRevoke(url: string): void {
  if (typeof URL !== "undefined" && typeof URL.revokeObjectURL === "function") {
    URL.revokeObjectURL(url);
  }
}

/**
 * The files being sent to a page: one entry each, kept until stored (the
 * editor then swaps in the block) or removed. The server owns the attachment;
 * nothing but its id ever reaches the page's content.
 */
export class AttachmentUploads {
  private readonly entries = new Map<string, Entry>();
  private readonly listeners = new Set<() => void>();
  private snapshotValue: readonly PendingUpload[] = [];
  private disposed = false;

  constructor(private readonly options: AttachmentUploadsOptions) {}

  /** Checks a file and starts sending it; a refused file starts nothing. */
  add(file: File): AddUploadResult {
    const refusal = refusePageFile(file);
    if (refusal) return { ok: false, refusal };
    const id = this.options.newId?.() ?? globalThis.crypto.randomUUID();
    const kind = isPageImage(file) ? "image" : "file";
    const previewUrl =
      kind === "image"
        ? (this.options.createPreviewUrl ?? browserPreview)(file)
        : null;
    this.entries.set(id, {
      file,
      idempotencyKey: globalThis.crypto.randomUUID(),
      controller: null,
      upload: {
        id,
        fileName: file.name,
        size: file.size,
        kind,
        status: "uploading",
        progress: null,
        failure: null,
        previewUrl,
      },
    });
    this.publish();
    this.send(id);
    return { ok: true, id };
  }

  /** Sends a failed file again with the same idempotency key. */
  retry(id: string): void {
    const entry = this.entries.get(id);
    if (!entry || entry.upload.status !== "failed") return;
    if (!canRetryUpload(entry.upload.failure)) return;
    this.send(id);
  }

  /** Drops an entry, cancelling its transfer. */
  remove(id: string): void {
    const entry = this.entries.get(id);
    if (!entry) return;
    entry.controller?.abort();
    this.forget(id);
  }

  get(id: string): PendingUpload | undefined {
    return this.entries.get(id)?.upload;
  }

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  /** The uploads now; a new array only when something changed. */
  snapshot = (): readonly PendingUpload[] => this.snapshotValue;

  dispose(): void {
    this.disposed = true;
    for (const entry of this.entries.values()) entry.controller?.abort();
    for (const id of [...this.entries.keys()]) this.forget(id);
    this.listeners.clear();
  }

  private forget(id: string): void {
    const entry = this.entries.get(id);
    if (!entry) return;
    if (entry.upload.previewUrl) {
      (this.options.revokePreviewUrl ?? browserRevoke)(entry.upload.previewUrl);
    }
    this.entries.delete(id);
    this.publish();
  }

  private patch(id: string, change: Partial<PendingUpload>): void {
    const entry = this.entries.get(id);
    if (!entry) return;
    entry.upload = { ...entry.upload, ...change };
    this.publish();
  }

  private publish(): void {
    this.snapshotValue = [...this.entries.values()].map(
      (entry) => entry.upload,
    );
    for (const listener of this.listeners) listener();
  }

  private send(id: string): void {
    const entry = this.entries.get(id);
    if (!entry) return;
    const controller = new AbortController();
    entry.controller = controller;
    this.patch(id, { status: "uploading", failure: null, progress: null });
    this.options
      .upload(entry.file, {
        idempotencyKey: entry.idempotencyKey,
        signal: controller.signal,
        onProgress: ({ loaded, total }) => {
          if (total > 0) this.patch(id, { progress: (loaded / total) * 100 });
        },
      })
      .then((attachment) => {
        if (this.disposed || !this.entries.has(id)) return;
        const done = entry.upload;
        this.options.onUploaded(done, attachment);
        this.forget(id);
      })
      .catch((error: unknown) => {
        if (isAborted(error) || !this.entries.has(id)) return;
        this.patch(id, {
          status: "failed",
          failure: classifyUploadFailure(error),
        });
      });
  }
}
