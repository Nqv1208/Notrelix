// The ambient typing of the prebuilt bundle must travel with this file into every program that compiles it.
// eslint-disable-next-line @typescript-eslint/triple-slash-reference
/// <reference path="./mammoth-browser.d.ts" />
import {
  parseMarkdown,
  sanitizeImport,
  type ImportedPage,
} from "@notrelix/docs-core";
import { htmlToImport } from "./html-to-blocks";

/** The largest file the import reads: 10 MB. */
export const IMPORT_MAX_BYTES = 10 * 1024 * 1024;

export type ImportFailure = "unsupported" | "tooLarge" | "empty" | "failed";

export type ImportRead =
  | { readonly ok: true; readonly title: string; readonly page: ImportedPage }
  | { readonly ok: false; readonly error: ImportFailure };

const MARKDOWN = /\.(md|markdown|txt)$/i;
const WORD = /\.docx$/i;

function titleOf(name: string): string {
  return name.replace(/\.[^.]+$/, "").trim();
}

/** Reads a file with `FileReader` (every browser, and the test DOM, has it). */
function readAs(file: File, as: "text"): Promise<string>;
function readAs(file: File, as: "buffer"): Promise<ArrayBuffer>;
function readAs(
  file: File,
  as: "text" | "buffer",
): Promise<string | ArrayBuffer> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string | ArrayBuffer);
    reader.onerror = () => reject(reader.error);
    if (as === "text") reader.readAsText(file);
    else reader.readAsArrayBuffer(file);
  });
}

/**
 * Reads a Markdown, text or Word file into sanitised blocks for a new page.
 * The Word reader is loaded only when a .docx is chosen.
 */
export async function readImportFile(file: File): Promise<ImportRead> {
  const isMarkdown = MARKDOWN.test(file.name);
  const isWord = WORD.test(file.name);
  if (!isMarkdown && !isWord) return { ok: false, error: "unsupported" };
  if (file.size > IMPORT_MAX_BYTES) return { ok: false, error: "tooLarge" };
  try {
    let page: ImportedPage;
    if (isMarkdown) {
      page = parseMarkdown(await readAs(file, "text"));
    } else {
      // The prebuilt browser bundle: it brings its own XML parser, so it does
      // not depend on the workspace's pinned `@xmldom/xmldom`.
      const { default: mammoth } =
        await import("mammoth/mammoth.browser.min.js");
      const converted = await mammoth.convertToHtml(
        { arrayBuffer: await readAs(file, "buffer") },
        // Images are not kept: nothing is embedded in the HTML.
        {
          convertImage: mammoth.images.imgElement(() =>
            Promise.resolve({ src: "" }),
          ),
        },
      );
      page = htmlToImport(converted.value);
    }
    const clean = sanitizeImport(page);
    if (!clean) return { ok: false, error: "tooLarge" };
    if (clean.blocks.length === 0) return { ok: false, error: "empty" };
    return {
      ok: true,
      title: clean.title ?? (titleOf(file.name) || file.name),
      page: clean,
    };
  } catch {
    return { ok: false, error: "failed" };
  }
}
