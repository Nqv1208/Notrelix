/**
 * What the item page checks before sending a file. The server decides: these
 * checks only spare a doomed upload (the limits mirror the API contract, see
 * backend api-and-contracts "Attachment file upload and download contract").
 */

/** The most a single file can be: 100 MB. */
export const MAX_ITEM_FILE_BYTES = 100 * 1024 * 1024;

/** Extensions the API refuses as executable or active content. */
export const BLOCKED_ITEM_FILE_EXTENSIONS: readonly string[] = [
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

export type ItemFileRefusal = "tooBig" | "blockedType";

/** Why a file should not be sent, or null when it may be. */
export function refuseItemFile(file: {
  readonly name: string;
  readonly size: number;
}): ItemFileRefusal | null {
  if (file.size > MAX_ITEM_FILE_BYTES) return "tooBig";
  const dot = file.name.lastIndexOf(".");
  const extension =
    dot < 0
      ? ""
      : file.name
          .slice(dot + 1)
          .trim()
          .toLowerCase();
  return BLOCKED_ITEM_FILE_EXTENSIONS.includes(extension)
    ? "blockedType"
    : null;
}
