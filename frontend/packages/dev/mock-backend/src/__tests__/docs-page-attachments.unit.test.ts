import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { createAttachmentApi } from "../../../../product/docs/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { buildOperationRegistry } from "../operations/build-registry";
import { createMockFetch } from "../transport/create-mock-fetch";

const BASE = "http://localhost:8000/api/v1";
const PAGE = INVOICE_DOC_ID;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const fetchImpl = createMockFetch(store);
  const client = createNotrelixClient({ baseUrl: BASE, fetchImpl });
  return {
    store,
    fetchImpl,
    attachments: createAttachmentApi(client.api, endpoints),
  };
}

const PNG = new Uint8Array([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 1, 2,
]);
const png = (name = "anh.png") =>
  new File([PNG], name, { type: "application/octet-stream" });
const pdf = (name = "ghi-chu.pdf") =>
  new File(["noi dung"], name, { type: "application/pdf" });
const key = () => ({ idempotencyKey: crypto.randomUUID() });

async function failure(promise: Promise<unknown>) {
  try {
    await promise;
  } catch (error) {
    return error as { status?: number; code?: string };
  }
  throw new Error("expected the request to fail");
}

describe("mock page attachments: Collaboration.PageAttachments.*", () => {
  it("registers the five operations under their OpenAPI operationIds", () => {
    const ids = buildOperationRegistry()
      .operationMetadata()
      .flatMap((m) =>
        m.contract.kind === "openapi" ? [m.contract.operationId] : [],
      );
    for (const op of ["Get", "Create", "UploadFile", "GetContent", "Delete"]) {
      expect(ids).toContain(`Collaboration.PageAttachments.${op}`);
    }
  });

  it("stores an image, lists it newest first and serves its bytes inline", async () => {
    const { attachments, fetchImpl } = setup();
    const stored = await attachments.upload(PAGE, png(), key());
    expect(stored.contentType).toBe("image/png");
    await attachments.upload(PAGE, pdf(), key());
    const list = await attachments.list(PAGE);
    expect(list.map((file) => file.fileName)).toEqual([
      "ghi-chu.pdf",
      "anh.png",
    ]);

    const response = await fetchImpl(
      `${BASE}/pages/${PAGE}/attachments/${stored.id}/content`,
    );
    expect(response.headers.get("Content-Type")).toBe("image/png");
    expect(response.headers.get("Content-Disposition")).toBe("inline");
    expect([...new Uint8Array(await response.arrayBuffer())]).toEqual([...PNG]);
  });

  it("serves a non-image as a download", async () => {
    const { attachments, fetchImpl } = setup();
    const stored = await attachments.upload(PAGE, pdf(), key());
    const response = await fetchImpl(
      `${BASE}/pages/${PAGE}/attachments/${stored.id}/content`,
    );
    expect(response.headers.get("Content-Disposition")).toMatch(/^attachment/);
  });

  it("replays an upload with the same idempotency key without a second file", async () => {
    const { attachments } = setup();
    const options = key();
    const first = await attachments.upload(PAGE, png(), options);
    const again = await attachments.upload(PAGE, png(), options);
    expect(again.id).toBe(first.id);
    expect(await attachments.list(PAGE)).toHaveLength(1);
  });

  it("refuses a blocked type (415), an empty file (400) and an oversized file (413)", async () => {
    const { attachments } = setup();
    expect(
      (
        await failure(
          attachments.upload(PAGE, new File(["x"], "run.exe"), key()),
        )
      ).status,
    ).toBe(415);
    expect(
      (await failure(attachments.upload(PAGE, new File([], "a.txt"), key())))
        .status,
    ).toBe(400);
    expect(
      (
        await failure(
          attachments.upload(
            PAGE,
            new File([new Uint8Array(100 * 1024 * 1024 + 1)], "lon.zip"),
            key(),
          ),
        )
      ).status,
    ).toBe(413);
    expect(await attachments.list(PAGE)).toEqual([]);
  });

  it("requires an Idempotency-Key", async () => {
    const { fetchImpl } = setup();
    const form = new FormData();
    form.append("file", pdf(), "a.pdf");
    const response = await fetchImpl(
      `${BASE}/pages/${PAGE}/attachments/files`,
      {
        method: "POST",
        body: form,
      },
    );
    expect(response.status).toBe(400);
  });

  it("deletes a file; it then no longer lists or reads", async () => {
    const { attachments } = setup();
    const stored = await attachments.upload(PAGE, pdf(), key());
    await attachments.remove(PAGE, stored.id);
    expect(await attachments.list(PAGE)).toEqual([]);
    expect((await failure(attachments.content(PAGE, stored.id))).status).toBe(
      404,
    );
    expect((await failure(attachments.remove(PAGE, stored.id))).status).toBe(
      404,
    );
  });

  it("hides an unknown page and never serves a file through another page", async () => {
    const { store, attachments } = setup();
    const other = store
      .getPages(store.getPage(PAGE)!.workspaceId)
      .find((page) => page.id !== PAGE)!;
    const stored = await attachments.upload(PAGE, pdf(), key());
    expect((await failure(attachments.list("no-such-page"))).status).toBe(404);
    expect(
      (await failure(attachments.content(other.id, stored.id))).status,
    ).toBe(404);
    expect(
      (await failure(attachments.remove(other.id, stored.id))).status,
    ).toBe(404);
    expect(await attachments.list(other.id)).toEqual([]);
  });

  it("does not serve an item's attachment through a page route", async () => {
    const { attachments } = setup();
    expect(
      (await failure(attachments.content(PAGE, "mock-erp-item-142"))).status,
    ).toBe(404);
  });

  describe("registers a link: Collaboration.PageAttachments.Create", () => {
    const link = (
      attachments: ReturnType<typeof setup>["attachments"],
      url = "https://example.com/guide",
      name = "Hướng dẫn",
      options = key(),
    ) => attachments.createLink(PAGE, { url, name }, options);

    it("lists the link newest first with its address, no bytes and no content route", async () => {
      const { attachments, fetchImpl } = setup();
      await attachments.upload(PAGE, pdf(), key());
      await link(attachments);
      const [first, second] = await attachments.list(PAGE);
      expect(first).toMatchObject({
        fileName: "Hướng dẫn",
        kind: "link",
        url: "https://example.com/guide",
      });
      expect(second?.kind).toBeUndefined();
      const response = await fetchImpl(
        `${BASE}/pages/${PAGE}/attachments/${first!.id}/content`,
      );
      expect(response.status).toBe(404);
    });

    it("refuses an address that is not absolute http or https (400) and a blank name", async () => {
      const { attachments } = setup();
      for (const bad of [
        "javascript:alert(1)",
        "data:text/html,x",
        "file:///etc/passwd",
        "mailto:a@b.c",
        "example.com",
        "https://",
      ]) {
        expect((await failure(link(attachments, bad))).status).toBe(400);
      }
      expect((await failure(link(attachments, undefined, " "))).status).toBe(
        400,
      );
      expect(await attachments.list(PAGE)).toEqual([]);
    });

    it("replays the same Idempotency-Key without a second link", async () => {
      const { attachments } = setup();
      const options = key();
      await link(attachments, undefined, undefined, options);
      await link(attachments, undefined, undefined, options);
      expect(await attachments.list(PAGE)).toHaveLength(1);
    });

    it("requires an Idempotency-Key", async () => {
      const { fetchImpl } = setup();
      const response = await fetchImpl(`${BASE}/pages/${PAGE}/attachments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ filename: "a", url: "https://example.com" }),
      });
      expect(response.status).toBe(400);
    });

    it("stops at 50 attachments on the page (409)", async () => {
      const { attachments } = setup();
      for (let index = 0; index < 50; index += 1) {
        await link(attachments, `https://example.com/${index}`);
      }
      const refused = await failure(link(attachments, "https://example.com/x"));
      expect(refused.status).toBe(409);
      expect(refused.code).toBe("attachment.limit_exceeded");
      expect(await attachments.list(PAGE)).toHaveLength(50);
    });

    it("refuses a viewer (403) and hides an unknown page (404)", async () => {
      const { store, attachments } = setup();
      expect(
        (
          await failure(
            attachments.createLink(
              "no-such-page",
              { url: "https://example.com", name: "a" },
              key(),
            ),
          )
        ).status,
      ).toBe(404);
      vi.spyOn(store, "getCurrentMemberRole").mockReturnValue("guest");
      const refused = await failure(link(attachments));
      expect(refused.status).toBe(403);
      expect(refused.code).toBe("collaboration.attachment_editor_only");
      vi.restoreAllMocks();
      expect(await attachments.list(PAGE)).toEqual([]);
    });
  });
});
