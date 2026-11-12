import { describe, expect, it } from "vitest";
import {
  checkPendingFiles,
  formatFileSize,
  glyphOf,
  isInlineImage,
  isRetryableUpload,
  mergeAttachment,
  uploadFailureOf,
} from "../core/model/attachment-model";
import { CHAT_LIMITS, type ChatAttachment } from "../core/types/chat";

function fileOf(name: string, size: number): File {
  const file = new File(["x"], name);
  Object.defineProperty(file, "size", { value: size });
  return file;
}

describe("checkPendingFiles", () => {
  it("accepts files up to the cap of 10 and leaves the rest out with the reason", () => {
    const incoming = Array.from({ length: 12 }, (_, i) => fileOf(`f${i}`, 5));
    const result = checkPendingFiles(0, incoming);
    expect(result.accepted).toHaveLength(10);
    expect(result.refused.map((entry) => entry.reason)).toEqual([
      "too-many",
      "too-many",
    ]);
  });

  it("counts the files already held", () => {
    const result = checkPendingFiles(9, [fileOf("a", 1), fileOf("b", 1)]);
    expect(result.accepted).toHaveLength(1);
    expect(result.refused).toEqual([{ name: "b", reason: "too-many" }]);
  });

  it("refuses an empty file and a file over 100 MB, and does not judge the type", () => {
    const result = checkPendingFiles(0, [
      fileOf("empty.txt", 0),
      fileOf("big.bin", CHAT_LIMITS.attachmentMaxBytes + 1),
      fileOf("exact.bin", CHAT_LIMITS.attachmentMaxBytes),
      fileOf("run.exe", 10),
    ]);
    expect(result.accepted.map((file) => file.name)).toEqual([
      "exact.bin",
      "run.exe",
    ]);
    expect(result.refused).toEqual([
      { name: "empty.txt", reason: "empty" },
      { name: "big.bin", reason: "too-large" },
    ]);
  });
});

describe("uploadFailureOf", () => {
  it("reads the server's reasons", () => {
    expect(uploadFailureOf({ status: 413 })).toBe("too-large");
    expect(uploadFailureOf({ status: 415 })).toBe("blocked");
    expect(
      uploadFailureOf({ status: 409, code: "attachment.limit_exceeded" }),
    ).toBe("limit");
    expect(uploadFailureOf({ status: 409, code: "channel-archived" })).toBe(
      "unavailable",
    );
    expect(uploadFailureOf({ status: 403 })).toBe("forbidden");
    expect(uploadFailureOf({ status: 400 })).toBe("invalid");
  });

  it("treats no answer, 429 and 5xx as worth another try, and a refusal as final", () => {
    expect(uploadFailureOf({ kind: "network" })).toBe("retryable");
    expect(uploadFailureOf({ status: 503 })).toBe("retryable");
    expect(uploadFailureOf(new Error("x"))).toBe("retryable");
    expect(isRetryableUpload("retryable")).toBe(true);
    expect(isRetryableUpload("blocked")).toBe(false);
    expect(isRetryableUpload("limit")).toBe(false);
  });
});

describe("file presentation", () => {
  it("formats sizes in binary steps", () => {
    expect(formatFileSize(0)).toBe("0 B");
    expect(formatFileSize(950)).toBe("950 B");
    expect(formatFileSize(1536)).toBe("1.5 KB");
    expect(formatFileSize(12 * 1024 * 1024)).toBe("12 MB");
    expect(formatFileSize(100 * 1024 * 1024)).toBe("100 MB");
    expect(formatFileSize(-1)).toBe("0 B");
  });

  it("picks the glyph from the type, then the extension", () => {
    expect(glyphOf("image/png", "a.png")).toBe("image");
    expect(glyphOf("application/pdf", "a")).toBe("pdf");
    expect(glyphOf("application/octet-stream", "backup.ZIP")).toBe("archive");
    expect(glyphOf("application/octet-stream", "sheet.xlsx")).toBe("sheet");
    expect(glyphOf("text/plain", "notes")).toBe("text");
    expect(glyphOf("application/octet-stream", "blob")).toBe("file");
  });

  it("previews only the four image types the server serves inline", () => {
    for (const type of ["image/png", "image/jpeg", "image/gif", "image/webp"]) {
      expect(isInlineImage(type)).toBe(true);
    }
    expect(isInlineImage("image/svg+xml")).toBe(false);
    expect(isInlineImage("application/pdf")).toBe(false);
  });
});

describe("mergeAttachment", () => {
  const item: ChatAttachment = {
    id: "a1",
    fileName: "a.txt",
    sizeBytes: 1,
    contentType: "text/plain",
    uploadedBy: "u",
    createdAt: "2026-11-10T09:00:00.000Z",
    downloadPath: "/x",
  };
  it("adds a file once", () => {
    expect(mergeAttachment(undefined, item)).toEqual([item]);
    const held = mergeAttachment([], item);
    expect(mergeAttachment(held, item)).toBe(held);
  });
});
