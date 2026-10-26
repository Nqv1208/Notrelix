import { describe, expect, it, vi } from "vitest";
import { createAttachmentApi } from "../api/attachment.api";
import type { DocsApiClient, DocsFileClient } from "../api/page.api";
import {
  decodeBlockBody,
  encodeContent,
  encodeProperties,
} from "../model/block-wire";
import { mapBlock } from "../model/block.mapper";
import { docsQueryKeys } from "../query/keys";
import { testEndpoints } from "./test-endpoints";

function clients() {
  const api = {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    upload: vi.fn(),
    getBlob: vi.fn(),
  };
  return api as typeof api & DocsApiClient & DocsFileClient;
}

describe("image and file block wire shapes", () => {
  it("reads an image block's attachment, width, alt and caption runs", () => {
    const body = decodeBlockBody("image", {
      content: {},
      properties: {
        attachmentId: "a1",
        width: 640,
        alt: "Sơ đồ",
        caption: [{ text: "Hình 1", marks: [{ type: "bold" }] }],
      },
    });
    expect(body.properties).toMatchObject({
      attachmentId: "a1",
      width: 640,
      alt: "Sơ đồ",
      caption: "Hình 1",
    });
  });

  it("writes an image block as attachmentId, width, alt and caption runs, never a url", () => {
    const draft = {
      type: "image" as const,
      content: { v: 1 as const, runs: [] },
      properties: {
        attachmentId: "a1",
        width: 320,
        alt: "x",
        caption: "Chú thích",
        url: "blob:http://localhost/abc",
      },
    };
    expect(encodeContent(draft)).toEqual({});
    expect(encodeProperties(draft)).toEqual({
      attachmentId: "a1",
      width: 320,
      alt: "x",
      caption: [{ text: "Chú thích", marks: [] }],
    });
  });

  it("round-trips a file block through the producer's File type", () => {
    const block = mapBlock(
      {
        id: "b1",
        pageId: "p1",
        type: "File",
        content: {},
        properties: { attachmentId: "f1" },
        position: "000000001000",
        version: 1,
        createdAt: "2026-10-26T00:00:00Z",
      },
      1000,
    );
    expect(block.type).toBe("file");
    expect(block.properties.attachmentId).toBe("f1");
    expect(
      encodeProperties({ type: "file", properties: block.properties }),
    ).toEqual({ attachmentId: "f1" });
    expect(
      encodeContent({ type: "file", content: { v: 1, runs: [] } }),
    ).toEqual({});
  });

  it("keeps an image block with no attachment yet empty", () => {
    expect(encodeProperties({ type: "image", properties: {} })).toEqual({});
  });
});

describe("page attachments api", () => {
  it("lists the page's files with their name, size and type", async () => {
    const api = clients();
    api.get.mockResolvedValue([
      {
        id: "a1",
        filename: "bao-cao.pdf",
        sizeBytes: 2048,
        contentType: "application/pdf",
        createdAt: "2026-10-26T01:00:00Z",
      },
      { filename: "no-id.txt" },
    ]);
    const list = await createAttachmentApi(api, testEndpoints).list("p1");
    expect(api.get).toHaveBeenCalledWith("/pages/p1/attachments");
    expect(list).toEqual([
      {
        id: "a1",
        fileName: "bao-cao.pdf",
        sizeBytes: 2048,
        contentType: "application/pdf",
        createdAt: "2026-10-26T01:00:00Z",
      },
    ]);
  });

  it("uploads as multipart part `file` with the caller's idempotency key", async () => {
    const api = clients();
    api.upload.mockResolvedValue({ id: "a2", filename: "a.png" });
    const file = new File([new Uint8Array(4)], "a.png", { type: "image/png" });
    const controller = new AbortController();
    const stored = await createAttachmentApi(api, testEndpoints).upload(
      "p1",
      file,
      { idempotencyKey: "key-1", signal: controller.signal },
    );
    const [url, form, options] = api.upload.mock.calls[0] as [
      string,
      FormData,
      { idempotencyKey: string },
    ];
    expect(url).toBe("/pages/p1/attachments/files");
    expect((form.get("file") as File).name).toBe("a.png");
    expect(options.idempotencyKey).toBe("key-1");
    expect(stored.id).toBe("a2");
  });

  it("reads bytes through the content route and deletes through the detail route", async () => {
    const api = clients();
    api.getBlob.mockResolvedValue(new Blob(["x"]));
    api.delete.mockResolvedValue(undefined);
    const attachments = createAttachmentApi(api, testEndpoints);
    await attachments.content("p1", "a1");
    await attachments.remove("p1", "a1");
    expect(api.getBlob).toHaveBeenCalledWith(
      "/pages/p1/attachments/a1/content",
    );
    expect(api.delete).toHaveBeenCalledWith("/pages/p1/attachments/a1");
  });

  it("scopes query keys by workspace and page", () => {
    expect(docsQueryKeys.attachments("w1", "p1")).not.toEqual(
      docsQueryKeys.attachments("w2", "p1"),
    );
    expect(docsQueryKeys.attachments("w1", "p1")).not.toEqual(
      docsQueryKeys.attachments("w1", "p2"),
    );
    expect(docsQueryKeys.attachmentContent("w1", "p1", "a1")).not.toEqual(
      docsQueryKeys.attachmentContent("w1", "p1", "a2"),
    );
  });
});
