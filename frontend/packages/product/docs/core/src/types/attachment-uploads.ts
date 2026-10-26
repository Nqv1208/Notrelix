import type { PageFileRefusal } from "./attachment-rules";

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

/** What the page's blocks read of the uploads in flight. */
export interface AttachmentUploadsView {
  /** Checks a file and starts sending it; a refused file starts nothing. */
  add(file: File): AddUploadResult;
  retry(id: string): void;
  /** Drops an upload, cancelling its transfer. */
  remove(id: string): void;
  get(id: string): PendingUpload | undefined;
  subscribe(listener: () => void): () => void;
  /** The uploads now; a new array only when something changed. */
  snapshot(): readonly PendingUpload[];
}
