import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../../../../product/work-management/state/src/index";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";
import { buildOperationRegistry } from "../operations/build-registry";

const ITEM = "mock-erp-item-142";
const OTHER_ITEM = "mock-erp-item-143";
const BASE = "http://localhost:8000/api/v1";

function clientFor(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: BASE,
    fetchImpl: createMockFetch(store),
  });
  return { store, cards: createCardApi(client) };
}

function postLink(
  store: MockStore,
  body: unknown,
  headers: Record<string, string> = { "Idempotency-Key": "key-1" },
  itemId = ITEM,
) {
  return createMockFetch(store)(`${BASE}/board-items/${itemId}/attachments`, {
    method: "POST",
    body: JSON.stringify(body),
    headers: { "Content-Type": "application/json", ...headers },
  });
}

async function failure(promise: Promise<unknown>) {
  try {
    await promise;
  } catch (error) {
    return error as { status?: number; code?: string };
  }
  throw new Error("expected the request to fail");
}

const LINK = { url: "https://example.com/spec", name: "Spec" };

describe("mock item link: Collaboration.Attachments.Create (UC-COL-B07)", () => {
  it("registers the create and delete operations under their OpenAPI operationIds", () => {
    const ids = buildOperationRegistry()
      .operationMetadata()
      .flatMap((m) =>
        m.contract.kind === "openapi" ? [m.contract.operationId] : [],
      );
    expect(ids).toContain("Collaboration.Attachments.Create");
    expect(ids).toContain("Collaboration.Attachments.Delete");
  });

  it("attaches a link that lists as a Link with the address and no size", async () => {
    const { cards } = clientFor();

    await cards.createCardLink(ITEM, LINK, { idempotencyKey: "key-1" });

    const added = (await cards.getCardFiles(ITEM)).find(
      (file) => file.name === "Spec",
    );
    expect(added).toMatchObject({
      kind: "link",
      url: "https://example.com/spec",
      source: "link",
      size: 0,
    });
  });

  it("ignores the size, content type and source a client sends", async () => {
    const { store, cards } = clientFor();

    const raw = await postLink(store, {
      filename: "Spec",
      url: "https://example.com/spec",
      sizeBytes: 999_999,
      contentType: "application/pdf",
      source: "upload",
    });

    expect(raw.status).toBeLessThan(300);
    const stored = store
      .getAttachments(ITEM)
      .find((a) => a.filename === "Spec");
    expect(stored).toMatchObject({
      sizeBytes: 0,
      contentType: "text/uri-list",
    });
    expect(
      (await cards.getCardFiles(ITEM)).find((file) => file.name === "Spec"),
    ).toMatchObject({ kind: "link", source: "link" });
  });

  it("replays the same key and payload without a second link", async () => {
    const { store } = clientFor();
    const before = store.getAttachments(ITEM).length;
    const body = { filename: "Spec", url: "https://example.com/spec" };

    const first = await postLink(store, body);
    const replay = await postLink(store, body);

    expect(replay.status).toBe(first.status);
    expect(store.getAttachments(ITEM)).toHaveLength(before + 1);
  });

  it("refuses the same key with another payload as 409 idempotency_payload_mismatch", async () => {
    const { store } = clientFor();
    await postLink(store, { filename: "Spec", url: "https://example.com/a" });

    const raw = await postLink(store, {
      filename: "Spec",
      url: "https://example.com/b",
    });

    expect(raw.status).toBe(409);
    expect((await raw.json()).code).toBe("idempotency_payload_mismatch");
  });

  it("requires the Idempotency-Key header", async () => {
    const { store } = clientFor();

    const raw = await postLink(
      store,
      { filename: "a", url: "https://a.io" },
      {},
    );

    expect(raw.status).toBe(400);
  });

  it.each([
    ["a script address", { filename: "x", url: "javascript:alert(1)" }],
    ["a relative address", { filename: "x", url: "/path" }],
    ["no name", { filename: " ", url: "https://example.com" }],
    ["no address", { filename: "x" }],
  ])("refuses %s with 400", async (_label, body) => {
    const { store } = clientFor();

    const raw = await postLink(store, body);

    expect(raw.status).toBe(400);
  });

  it("refuses the 51st attachment with 409 attachment.limit_exceeded", async () => {
    const { store, cards } = clientFor();
    for (let n = store.getAttachments(ITEM).length; n < 50; n++)
      await cards.createCardLink(
        ITEM,
        { url: `https://example.com/${n}`, name: `l${n}` },
        { idempotencyKey: `key-${n}` },
      );

    expect(
      await failure(
        cards.createCardLink(ITEM, LINK, { idempotencyKey: "key-last" }),
      ),
    ).toMatchObject({ status: 409, code: "attachment.limit_exceeded" });
  });

  it("answers 403 to a viewer and 404 for an unknown item", async () => {
    const viewer = clientFor("viewer");
    expect(
      await failure(
        viewer.cards.createCardLink(ITEM, LINK, { idempotencyKey: "k" }),
      ),
    ).toMatchObject({ status: 403 });
    const owner = clientFor();
    expect(
      await failure(
        owner.cards.createCardLink("nope", LINK, { idempotencyKey: "k" }),
      ),
    ).toMatchObject({ status: 404 });
  });
});

describe("mock item attachment delete: Collaboration.Attachments.Delete (UC-COL-B11)", () => {
  it("removes the attachment from the list", async () => {
    const { cards } = clientFor();
    await cards.createCardLink(ITEM, LINK, { idempotencyKey: "key-1" });
    const added = (await cards.getCardFiles(ITEM)).find(
      (file) => file.name === "Spec",
    )!;

    await cards.deleteCardFile(ITEM, added.id);

    expect(
      (await cards.getCardFiles(ITEM)).map((file) => file.id),
    ).not.toContain(added.id);
  });

  it("answers 404 for an unknown attachment, another item's attachment and an unknown item", async () => {
    const { cards } = clientFor();
    await cards.createCardLink(ITEM, LINK, { idempotencyKey: "key-1" });
    const added = (await cards.getCardFiles(ITEM)).find(
      (file) => file.name === "Spec",
    )!;

    expect(await failure(cards.deleteCardFile(ITEM, "nope"))).toMatchObject({
      status: 404,
    });
    expect(
      await failure(cards.deleteCardFile(OTHER_ITEM, added.id)),
    ).toMatchObject({ status: 404 });
    expect(await failure(cards.deleteCardFile("nope", added.id))).toMatchObject(
      {
        status: 404,
      },
    );
  });

  it("answers 403 to someone who can see the item but not edit it, and keeps the file", async () => {
    const viewer = clientFor("viewer");
    const id = viewer.store.getAttachments(ITEM)[0]!.id;

    expect(await failure(viewer.cards.deleteCardFile(ITEM, id))).toMatchObject({
      status: 403,
    });
    expect(viewer.store.getAttachments(ITEM).map((a) => a.id)).toContain(id);
  });
});
