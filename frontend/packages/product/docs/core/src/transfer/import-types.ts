import type { BlockProperties, BlockType } from "../types/block";
import type { RichTextV1 } from "../types/rich-text";

/** A block read from a file, before it is written to a new page. */
export interface ImportedBlock {
  readonly type: BlockType;
  readonly content: RichTextV1;
  readonly properties: BlockProperties;
}

/** A file read into a page's worth of blocks. */
export interface ImportedPage {
  /** The first top-level heading (Markdown) when the file has one. */
  readonly title: string | null;
  readonly blocks: ImportedBlock[];
  /** Images the file had that were not imported. */
  readonly imagesDropped: number;
}

/** A page takes at most this many imported blocks. */
export const MAX_IMPORT_BLOCKS = 5000;
