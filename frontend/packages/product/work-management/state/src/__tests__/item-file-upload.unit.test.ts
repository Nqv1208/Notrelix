import { describe, expect, it, vi } from "vitest";
import { endpoints, type NotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../api/item.api";
import { mapAttachmentDtoToCardFile } from "../api/board-api-mappers";
import {
  canRetryItemFileUpload,
  classifyItemFileUploadError,
  type ItemFileUpload,
} from "../mutations/use-item-file-uploads";

const dto = {
  id: "att-1",
  resourceId: "item-1",
  filename: "ghi-chu.pdf",
  url: "",
  kind: "File",
  sizeBytes: 8,
  contentType: "application/pdf",
  source: "upload",
  uploadedBy: "user-1",
  uploadedByName: "Trần Minh Quân",
  createdAt: "2026-10-22T03:00:00.000Z",
};

function clientWith(upload = vi.fn(async () => dto), getBlob = vi.fn()) {
  const client = {
    api: { upload, getBlob },
    endpoints,
  } as unknown as NotrelixClient;
  return { api: createCardApi(client), upload, getBlob };
}

describe("cards.uploadCardFile", () => {
  it("posts one multipart part named file, with its filename, to the files route", async () => {
    const { api, upload } = clientWith();
    const file = new File(["noi dung"], "ghi-chu.pdf", {
      type: "application/pdf",
    });
    const onProgress = vi.fn();
    const signal = new AbortController().signal;
    await api.uploadCardFile("item-1", file, {
      idempotencyKey: "key-1",
      signal,
      onProgress,
    });
    const [url, form, options] = upload.mock.calls[0] as unknown as [
      string,
      FormData,
      Record<string, unknown>,
    ];
    expect(url).toBe("/board-items/item-1/attachments/files");
    expect([...form.keys()]).toEqual(["file"]);
    const part = form.get("file") as File;
    expect(part.name).toBe("ghi-chu.pdf");
    expect(part.size).toBe(file.size);
    expect(options).toEqual({ idempotencyKey: "key-1", signal, onProgress });
  });

  it("returns the stored file as a downloadable CardFile (no public url)", async () => {
    const { api } = clientWith();
    const stored = await api.uploadCardFile(
      "item-1",
      new File(["x"], "ghi-chu.pdf"),
      { idempotencyKey: "key-1" },
    );
    expect(stored).toMatchObject({
      id: "att-1",
      cardId: "item-1",
      name: "ghi-chu.pdf",
      url: "",
      kind: "file",
      source: "upload",
    });
  });

  it("lets the caller reuse one key to retry the same action", async () => {
    const upload = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(dto);
    const { api } = clientWith(upload as never);
    const file = new File(["x"], "a.pdf");
    await expect(
      api.uploadCardFile("item-1", file, { idempotencyKey: "action-1" }),
    ).rejects.toThrow("network");
    await api.uploadCardFile("item-1", file, { idempotencyKey: "action-1" });
    const keys = upload.mock.calls.map((call) => call[2].idempotencyKey);
    expect(keys).toEqual(["action-1", "action-1"]);
  });
});

describe("cards.downloadCardFile", () => {
  it("reads the content route of the item's attachment", async () => {
    const blob = new Blob(["x"]);
    const { api, getBlob } = clientWith(
      undefined,
      vi.fn(async () => blob),
    );
    await expect(api.downloadCardFile("item-1", "att-1")).resolves.toBe(blob);
    expect(getBlob.mock.calls[0]![0]).toBe(
      "/board-items/item-1/attachments/att-1/content",
    );
  });
});

describe("mapAttachmentDtoToCardFile", () => {
  it("keeps a link on its url and a file off any url", () => {
    const link = mapAttachmentDtoToCardFile(
      { ...dto, kind: "Link", url: "https://example.com/a", source: "link" },
      "item-1",
    );
    expect(link).toMatchObject({
      kind: "link",
      url: "https://example.com/a",
      source: "link",
    });
    const file = mapAttachmentDtoToCardFile(
      { ...dto, url: "https://ignored.example" },
      "item-1",
    );
    expect(file).toMatchObject({ kind: "file", url: "", source: "upload" });
  });

  it("treats a record without a kind as a link", () => {
    const old = mapAttachmentDtoToCardFile(
      { ...dto, kind: undefined, url: "https://example.com/a", source: "link" },
      "item-1",
    );
    expect(old.kind).toBe("link");
  });
});

describe("classifyItemFileUploadError", () => {
  it.each([
    [{ status: 413, code: "attachment.payload_too_large" }, "serverTooLarge"],
    [{ status: 413 }, "serverTooLarge"],
    [
      { status: 415, code: "attachment.unsupported_media_type" },
      "unsupportedType",
    ],
    [{ status: 409, code: "attachment.limit_exceeded" }, "limitReached"],
    [{ status: 409, code: "other" }, "failed"],
    [{ status: 403 }, "forbidden"],
    [{ status: 404 }, "notFound"],
    [{ kind: "network" }, "failed"],
    [{ status: 503 }, "failed"],
    [new Error("boom"), "failed"],
    [null, "failed"],
  ])("maps %j to %s", (error, expected) => {
    expect(classifyItemFileUploadError(error)).toBe(expected);
  });
});

describe("canRetryItemFileUpload", () => {
  const row = (error: ItemFileUpload["error"]): ItemFileUpload => ({
    id: "u",
    name: "a",
    size: 1,
    status: "error",
    progress: null,
    error,
  });
  it("retries a broken transfer and a full item, not a refused file", () => {
    expect(canRetryItemFileUpload(row("failed"))).toBe(true);
    expect(canRetryItemFileUpload(row("limitReached"))).toBe(true);
    for (const error of [
      "tooBig",
      "blockedType",
      "serverTooLarge",
      "unsupportedType",
      "forbidden",
      "notFound",
    ] as const)
      expect(canRetryItemFileUpload(row(error))).toBe(false);
    expect(canRetryItemFileUpload({ ...row(null), status: "uploading" })).toBe(
      false,
    );
  });
});
