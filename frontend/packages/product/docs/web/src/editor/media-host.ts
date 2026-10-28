import type {
  AttachmentUploadsView,
  PageAttachment,
  PageFileRefusal,
} from "@notrelix/docs-core";

/** The page's files as the image and file blocks read them. */
export interface DocMediaFiles {
  readonly files: ReadonlyMap<string, PageAttachment>;
  readonly loading: boolean;
}

/** The bytes of one stored file, read through the authorised content route. */
export interface DocMediaContent {
  readonly status: "loading" | "error" | "ready";
  readonly blob: Blob | null;
  readonly retry: () => void;
}

/**
 * What the image and file blocks need from the page: the uploads in flight,
 * the page's stored files and their bytes. The page screen supplies it; an
 * editor without it offers no image or file blocks.
 */
export interface DocMediaHost {
  readonly uploads: AttachmentUploadsView;
  /** A file was refused before sending (too big, blocked type, empty). */
  readonly onRefused: (file: File, refusal: PageFileRefusal) => void;
  /** Hooks: called from the blocks' views, always in the same order. */
  readonly useFiles: () => DocMediaFiles;
  readonly useContent: (attachmentId: string | null) => DocMediaContent;
  /** Saves a stored file to the member's device. */
  readonly download: (attachment: PageAttachment) => Promise<void>;
}
