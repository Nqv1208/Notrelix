import { describe, expect, it, vi } from "vitest";
import type { PageAttachment } from "@notrelix/docs-core";
import {
  AttachmentUploads,
  canRetryUpload,
  classifyUploadFailure,
} from "../editor/attachment-uploads";

const STORED: PageAttachment = {
  id: "a1",
  fileName: "anh.png",
  sizeBytes: 4,
  contentType: "image/png",
  createdAt: "2026-10-26T00:00:00Z",
};

function png(name = "anh.png") {
  return new File([new Uint8Array(4)], name, { type: "image/png" });
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

function setup(upload = vi.fn()) {
  const onUploaded = vi.fn();
  const revoked: string[] = [];
  const uploads = new AttachmentUploads({
    upload,
    onUploaded,
    createPreviewUrl: () => "blob:preview",
    revokePreviewUrl: (url) => revoked.push(url),
  });
  return { uploads, onUploaded, revoked, upload };
}

const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

describe("AttachmentUploads", () => {
  it("uploads an image, reports progress and hands the stored file to the editor", async () => {
    const pending = deferred<PageAttachment>();
    const { uploads, onUploaded, revoked, upload } = setup(
      vi.fn(() => pending.promise),
    );
    const added = uploads.add(png());
    expect(added.ok).toBe(true);
    const id = (added as { id: string }).id;
    expect(uploads.get(id)).toMatchObject({
      kind: "image",
      status: "uploading",
      previewUrl: "blob:preview",
    });
    const options = upload.mock.calls[0]?.[1] as {
      onProgress: (p: { loaded: number; total: number }) => void;
    };
    options.onProgress({ loaded: 1, total: 4 });
    expect(uploads.get(id)?.progress).toBe(25);

    pending.resolve(STORED);
    await settle();
    expect(onUploaded).toHaveBeenCalledWith(
      expect.objectContaining({ id, kind: "image" }),
      STORED,
    );
    expect(uploads.get(id)).toBeUndefined();
    expect(revoked).toEqual(["blob:preview"]);
  });

  it("treats any other file as a file block with no preview", () => {
    const { uploads } = setup(vi.fn(() => new Promise(() => undefined)));
    const result = uploads.add(
      new File([new Uint8Array(3)], "bao-cao.pdf", { type: "application/pdf" }),
    );
    expect(uploads.get((result as { id: string }).id)).toMatchObject({
      kind: "file",
      previewUrl: null,
    });
  });

  it("refuses a blocked extension, an empty file and an oversized file before sending", () => {
    const { uploads, upload } = setup();
    expect(uploads.add(new File([new Uint8Array(3)], "run.exe"))).toEqual({
      ok: false,
      refusal: "blockedType",
    });
    expect(uploads.add(new File([], "empty.txt"))).toEqual({
      ok: false,
      refusal: "empty",
    });
    const big = new File([new Uint8Array(1)], "big.zip");
    Object.defineProperty(big, "size", { value: 100 * 1024 * 1024 + 1 });
    expect(uploads.add(big)).toEqual({ ok: false, refusal: "tooBig" });
    expect(upload).not.toHaveBeenCalled();
    expect(uploads.snapshot()).toEqual([]);
  });

  it("keeps a failed upload for Retry, which resends with the same idempotency key", async () => {
    const upload = vi
      .fn()
      .mockRejectedValueOnce({ kind: "network" })
      .mockResolvedValueOnce(STORED);
    const { uploads, onUploaded } = setup(upload);
    const { id } = uploads.add(png()) as { id: string };
    await settle();
    expect(uploads.get(id)).toMatchObject({
      status: "failed",
      failure: "failed",
    });
    expect(onUploaded).not.toHaveBeenCalled();

    uploads.retry(id);
    await settle();
    expect(onUploaded).toHaveBeenCalledTimes(1);
    const keys = upload.mock.calls.map(
      (call) => (call[1] as { idempotencyKey: string }).idempotencyKey,
    );
    expect(keys[0]).toBe(keys[1]);
  });

  it("offers no retry for an answer another attempt cannot change", async () => {
    const upload = vi.fn().mockRejectedValue({ status: 415 });
    const { uploads } = setup(upload);
    const { id } = uploads.add(png()) as { id: string };
    await settle();
    expect(uploads.get(id)?.failure).toBe("unsupportedType");
    uploads.retry(id);
    await settle();
    expect(upload).toHaveBeenCalledTimes(1);
  });

  it("removes a failed upload and drops its preview", async () => {
    const { uploads, revoked } = setup(vi.fn().mockRejectedValue({}));
    const { id } = uploads.add(png()) as { id: string };
    await settle();
    uploads.remove(id);
    expect(uploads.get(id)).toBeUndefined();
    expect(revoked).toEqual(["blob:preview"]);
  });

  it("cancels the transfer of an upload that is removed or disposed", () => {
    const signals: AbortSignal[] = [];
    const upload = vi.fn((_file: File, options: { signal: AbortSignal }) => {
      signals.push(options.signal);
      return new Promise<PageAttachment>(() => undefined);
    });
    const { uploads } = setup(upload as never);
    const { id } = uploads.add(png()) as { id: string };
    uploads.add(png("b.png"));
    uploads.remove(id);
    expect(signals[0]?.aborted).toBe(true);
    uploads.dispose();
    expect(signals[1]?.aborted).toBe(true);
  });

  it("notifies subscribers with a new snapshot on each change", () => {
    const { uploads } = setup(vi.fn(() => new Promise(() => undefined)));
    const listener = vi.fn();
    uploads.subscribe(listener);
    const before = uploads.snapshot();
    uploads.add(png());
    expect(listener).toHaveBeenCalled();
    expect(uploads.snapshot()).not.toBe(before);
  });
});

describe("upload failures", () => {
  it("maps the API's answers to reasons", () => {
    expect(classifyUploadFailure({ status: 413 })).toBe("serverTooLarge");
    expect(
      classifyUploadFailure({ code: "attachment.unsupported_media_type" }),
    ).toBe("unsupportedType");
    expect(
      classifyUploadFailure({ status: 409, code: "attachment.limit_exceeded" }),
    ).toBe("limitReached");
    expect(classifyUploadFailure({ status: 403 })).toBe("forbidden");
    expect(classifyUploadFailure({ status: 404 })).toBe("notFound");
    expect(classifyUploadFailure(new Error("x"))).toBe("failed");
  });

  it("retries only what another attempt can change", () => {
    expect(canRetryUpload("failed")).toBe(true);
    expect(canRetryUpload("limitReached")).toBe(true);
    expect(canRetryUpload("serverTooLarge")).toBe(false);
    expect(canRetryUpload(null)).toBe(false);
  });
});
