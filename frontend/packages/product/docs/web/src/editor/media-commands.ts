import type { Editor, JSONContent } from "@tiptap/core";
import type { PageAttachment, PendingUpload } from "@notrelix/docs-core";
import { UPLOAD_PLACEHOLDER, newBlockId } from "./block-mapping";
import type { DocMediaHost } from "./media-host";

/** What the editor keeps for its media extension. */
export interface DocMediaStorage {
  /** The page's media host; absent when the page offers no image or file blocks. */
  host: () => DocMediaHost | undefined;
}

export function mediaOf(editor: Editor): DocMediaHost | undefined {
  return (editor.storage as { docMedia?: DocMediaStorage }).docMedia?.host();
}

/** Where a block inserted for `files` goes: over an empty block, else after the current one. */
function insertionRange(
  editor: Editor,
  at?: number,
): { from: number; to: number } {
  const { doc, selection } = editor.state;
  if (at !== undefined) {
    const $at = doc.resolve(Math.min(Math.max(0, at), doc.content.size));
    const top = $at.depth === 0 ? $at.pos : $at.before(1);
    return { from: top, to: top };
  }
  const { $from } = selection;
  if ($from.depth === 0) return { from: $from.pos, to: $from.pos };
  const block = $from.node(1);
  const start = $from.before(1);
  if (block.isTextblock && block.content.size === 0) {
    return { from: start, to: start + block.nodeSize };
  }
  const end = start + block.nodeSize;
  return { from: end, to: end };
}

/**
 * Starts the upload of each file and puts a placeholder for it in the page.
 * A file the page refuses (too big, blocked type, empty) is reported and
 * leaves nothing behind. Returns how many uploads started.
 */
export function insertMediaFiles(
  editor: Editor,
  files: readonly File[],
  at?: number,
): number {
  const media = mediaOf(editor);
  if (!media || !editor.isEditable) return 0;
  const nodes: JSONContent[] = [];
  for (const file of files) {
    const result = media.uploads.add(file);
    if (!result.ok) {
      media.onRefused(file, result.refusal);
      continue;
    }
    const upload = media.uploads.get(result.id);
    nodes.push({
      type: UPLOAD_PLACEHOLDER,
      attrs: {
        blockId: newBlockId(),
        uploadId: result.id,
        kind: upload?.kind ?? "file",
        fileName: file.name,
      },
    });
  }
  if (nodes.length === 0) return 0;
  const range = insertionRange(editor, at);
  const atEnd = range.to >= editor.state.doc.content.size;
  const content: JSONContent[] = atEnd
    ? [...nodes, { type: "paragraph", attrs: { blockId: newBlockId() } }]
    : nodes;
  editor.chain().focus().insertContentAt(range, content).run();
  return nodes.length;
}

/** The position of the placeholder an upload waits in. */
function placeholderPos(editor: Editor, uploadId: string): number | null {
  let found: number | null = null;
  editor.state.doc.forEach((node, offset) => {
    if (
      node.type.name === UPLOAD_PLACEHOLDER &&
      node.attrs.uploadId === uploadId
    ) {
      found = offset;
    }
  });
  return found;
}

/**
 * The upload is stored: its placeholder becomes the image or file block that
 * points at the attachment (the block keeps the placeholder's id). An upload
 * whose placeholder is gone (removed or undone) leaves the file stored and no
 * block.
 */
export function completeUpload(
  editor: Editor,
  upload: Pick<PendingUpload, "id" | "kind">,
  attachment: PageAttachment,
): boolean {
  if (editor.isDestroyed) return false;
  const pos = placeholderPos(editor, upload.id);
  if (pos === null) return false;
  const node = editor.state.doc.nodeAt(pos);
  if (!node) return false;
  const next = editor.schema.nodes[
    upload.kind === "image" ? "imageBlock" : "fileBlock"
  ]!.create({
    blockId: node.attrs.blockId,
    attachmentId: attachment.id,
  });
  editor.view.dispatch(
    editor.state.tr.replaceWith(pos, pos + node.nodeSize, next),
  );
  return true;
}

/** Removes a placeholder (the member dropped the upload). */
export function removePlaceholder(editor: Editor, uploadId: string): void {
  if (editor.isDestroyed) return;
  const pos = placeholderPos(editor, uploadId);
  if (pos === null) return;
  const node = editor.state.doc.nodeAt(pos);
  if (!node) return;
  editor.view.dispatch(editor.state.tr.delete(pos, pos + node.nodeSize));
}

const IMAGE_ACCEPT = "image/png,image/jpeg,image/gif,image/webp";

/** Opens the device's file chooser; resolves with the chosen files. */
export function chooseFiles(
  accept: string,
  multiple: boolean,
): Promise<File[]> {
  return new Promise((resolve) => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = accept;
    input.multiple = multiple;
    input.addEventListener(
      "change",
      () => resolve(Array.from(input.files ?? [])),
      {
        once: true,
      },
    );
    input.addEventListener("cancel", () => resolve([]), { once: true });
    input.click();
  });
}

/** The chooser for an image or a file block, then the upload of what was chosen. */
export async function pickMedia(
  editor: Editor,
  kind: "image" | "file",
): Promise<number> {
  const files = await chooseFiles(kind === "image" ? IMAGE_ACCEPT : "", true);
  if (files.length === 0 || editor.isDestroyed) return 0;
  return insertMediaFiles(editor, files);
}
