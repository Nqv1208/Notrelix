import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../../../../product/work-management/state/src/index";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";
import { buildOperationRegistry } from "../operations/build-registry";

const ITEM = "mock-erp-item-142";
const BASE = "http://localhost:8000/api/v1";

function clientFor(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: BASE,
    fetchImpl: createMockFetch(store),
  });
  return { store, client, cards: createCardApi(client) };
}

const pdf = (name = "ghi-chu.pdf", body = "noi dung") =>
  new File([body], name, { type: "application/pdf" });

async function failure(promise: Promise<unknown>) {
  try {
    await promise;
  } catch (error) {
    return error as { status?: number; code?: string };
  }
  throw new Error("expected the request to fail");
}

describe("mock attachments: Collaboration.Attachments.UploadFile / GetContent", () => {
  it("registers both operations under their OpenAPI operationIds", () => {
    const ids = buildOperationRegistry()
      .operationMetadata()
      .flatMap((m) =>
        m.contract.kind === "openapi" ? [m.contract.operationId] : [],
      );
    expect(ids).toContain("Collaboration.Attachments.UploadFile");
    expect(ids).toContain("Collaboration.Attachments.GetContent");
  });

  it("stores an upload and answers 201 with the AttachmentDto (Kind File, no Url) and the content Location", async () => {
    const { store, client } = clientFor();
    const form = new FormData();
    form.append("file", pdf(), "ghi-chu.pdf");
    const raw = await createMockFetch(store)(
      `${BASE}/board-items/${ITEM}/attachments/files`,
      {
        method: "POST",
        body: form,
        headers: { "Idempotency-Key": "key-1" },
      },
    );
    expect(raw.status).toBe(201);
    const dto = await raw.json();
    expect(dto).toMatchObject({
      filename: "ghi-chu.pdf",
      kind: "File",
      url: "",
      source: "upload",
      resourceId: ITEM,
      sizeBytes: 8,
    });
    expect(raw.headers.get("Location")).toBe(
      `/api/v1/board-items/${ITEM}/attachments/${dto.id}/content`,
    );
    expect(store.getAttachments(ITEM).map((file) => file.filename)).toContain(
      "ghi-chu.pdf",
    );
    void client;
  });

  it("lists uploaded and seeded files as Kind File with no Url", async () => {
    const { cards } = clientFor();
    await cards.uploadCardFile(ITEM, pdf(), { idempotencyKey: "key-1" });
    const files = await cards.getCardFiles(ITEM);
    expect(files.length).toBe(4);
    expect(files.every((file) => file.kind === "file" && file.url === "")).toBe(
      true,
    );
  });

  it("replays the same key without a second file and marks the replay", async () => {
    const { store } = clientFor();
    const send = () => {
      const form = new FormData();
      form.append("file", pdf(), "ghi-chu.pdf");
      return createMockFetch(store)(
        `${BASE}/board-items/${ITEM}/attachments/files`,
        { method: "POST", body: form, headers: { "Idempotency-Key": "same" } },
      );
    };
    const first = await send();
    const replay = await send();
    expect(replay.status).toBe(201);
    expect(replay.headers.get("Idempotency-Replayed")).toBe("true");
    expect((await replay.json()).id).toBe((await first.json()).id);
    expect(store.getAttachments(ITEM)).toHaveLength(4);
  });

  it("streams a stub body for the content route, with its headers", async () => {
    const { store, cards } = clientFor();
    const stored = await cards.uploadCardFile(ITEM, pdf(), {
      idempotencyKey: "key-1",
    });
    const raw = await createMockFetch(store)(
      `${BASE}/board-items/${ITEM}/attachments/${stored.id}/content`,
      { method: "GET" },
    );
    expect(raw.status).toBe(200);
    expect(raw.headers.get("X-Content-Type-Options")).toBe("nosniff");
    expect(raw.headers.get("Cache-Control")).toBe("private, no-store");
    expect(raw.headers.get("Content-Disposition")).toBe("attachment");
    expect(await raw.text()).toContain("ghi-chu.pdf");
    const blob = await cards.downloadCardFile(ITEM, stored.id);
    expect(await blob.text()).toContain("ghi-chu.pdf");
  });

  it("answers 404 for an unknown attachment, another item's attachment and an unknown item", async () => {
    const { cards } = clientFor();
    const stored = await cards.uploadCardFile(ITEM, pdf(), {
      idempotencyKey: "key-1",
    });
    expect(await failure(cards.downloadCardFile(ITEM, "nope"))).toMatchObject({
      status: 404,
    });
    expect(
      await failure(cards.downloadCardFile("mock-erp-item-143", stored.id)),
    ).toMatchObject({ status: 404 });
    expect(
      await failure(cards.downloadCardFile("nope", stored.id)),
    ).toMatchObject({ status: 404 });
  });

  it("refuses a missing file part and an empty file with 400", async () => {
    const { cards, store } = clientFor();
    const empty = await failure(
      cards.uploadCardFile(ITEM, new File([], "vuong.pdf"), {
        idempotencyKey: "key-1",
      }),
    );
    expect(empty.status).toBe(400);
    const raw = await createMockFetch(store)(
      `${BASE}/board-items/${ITEM}/attachments/files`,
      {
        method: "POST",
        body: new FormData(),
        headers: { "Idempotency-Key": "key-2" },
      },
    );
    expect(raw.status).toBe(400);
  });

  it("refuses a file over 100 MB with 413 attachment.payload_too_large", async () => {
    const { cards, store } = clientFor();
    const error = await failure(
      cards.uploadCardFile(
        ITEM,
        new File([new Uint8Array(100 * 1024 * 1024 + 1)], "lon.zip"),
        { idempotencyKey: "key-1" },
      ),
    );
    expect(error).toMatchObject({
      status: 413,
      code: "attachment.payload_too_large",
    });
    expect(store.getAttachments(ITEM)).toHaveLength(3);
  });

  it.each([
    "setup.exe",
    "a.MSI",
    "x.dll",
    "run.bat",
    "run.cmd",
    "a.com",
    "a.scr",
    "a.ps1",
    "a.vbs",
    "a.sh",
    "a.jar",
    "a.apk",
    "a.js",
    "a.mjs",
    "a.html",
    "a.htm",
  ])("refuses %s with 415 attachment.unsupported_media_type", async (name) => {
    const { cards } = clientFor();
    expect(
      await failure(
        cards.uploadCardFile(ITEM, new File(["x"], name), {
          idempotencyKey: "key-1",
        }),
      ),
    ).toMatchObject({
      status: 415,
      code: "attachment.unsupported_media_type",
    });
  });

  it("refuses the 51st attachment with 409 attachment.limit_exceeded", async () => {
    const { cards, store } = clientFor();
    for (let n = store.getAttachments(ITEM).length; n < 50; n++)
      await cards.uploadCardFile(ITEM, pdf(`f${n}.pdf`), {
        idempotencyKey: `key-${n}`,
      });
    expect(store.getAttachments(ITEM)).toHaveLength(50);
    expect(
      await failure(
        cards.uploadCardFile(ITEM, pdf("one-more.pdf"), {
          idempotencyKey: "key-last",
        }),
      ),
    ).toMatchObject({ status: 409, code: "attachment.limit_exceeded" });
  });

  it("answers 403 to someone who can see the item but not edit it, and 404 for an unknown item", async () => {
    const commenter = clientFor("commenter");
    expect(
      await failure(
        commenter.cards.uploadCardFile(ITEM, pdf(), { idempotencyKey: "k" }),
      ),
    ).toMatchObject({ status: 403 });
    const owner = clientFor();
    expect(
      await failure(
        owner.cards.uploadCardFile("nope", pdf(), { idempotencyKey: "k" }),
      ),
    ).toMatchObject({ status: 404 });
  });

  it("requires the Idempotency-Key header", async () => {
    const { store } = clientFor();
    const form = new FormData();
    form.append("file", pdf(), "ghi-chu.pdf");
    const raw = await createMockFetch(store)(
      `${BASE}/board-items/${ITEM}/attachments/files`,
      { method: "POST", body: form },
    );
    expect(raw.status).toBe(400);
  });
});
