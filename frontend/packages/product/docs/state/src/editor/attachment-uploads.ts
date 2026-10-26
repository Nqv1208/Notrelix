import {
  canRetryUpload,
  isPageImage,
  refusePageFile,
  type AddUploadResult,
  type AttachmentUploadsView,
  type PageAttachment,
  type PendingUpload,
  type UploadFailure,
} from "@notrelix/docs-core";
import type { DocsUploadOptions } from "../api/page.api";

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
  /** An upload failed for good or until retried: the member is told once. */
  readonly onFailed?: (upload: PendingUpload, failure: UploadFailure) => void;
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
export class AttachmentUploads implements AttachmentUploadsView {
  private readonly entries = new Map<string, Entry>();
  private readonly listeners = new Set<() => void>();
  private snapshotValue: readonly PendingUpload[] = [];

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

  /** Cancels every transfer and forgets the uploads; the object stays usable. */
  dispose(): void {
    for (const entry of this.entries.values()) entry.controller?.abort();
    for (const id of [...this.entries.keys()]) this.forget(id);
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
        if (!this.entries.has(id)) return;
        const done = entry.upload;
        this.options.onUploaded(done, attachment);
        this.forget(id);
      })
      .catch((error: unknown) => {
        if (isAborted(error) || !this.entries.has(id)) return;
        const failure = classifyUploadFailure(error);
        this.patch(id, { status: "failed", failure });
        const failed = this.entries.get(id)?.upload;
        if (failed) this.options.onFailed?.(failed, failure);
      });
  }
}

export { canRetryUpload };
export type { AddUploadResult, PendingUpload, UploadFailure };
