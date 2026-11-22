import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../api/item.api";
import { mapAttachmentDtoToCardFile } from "../api/board-api-mappers";
import {
  classifyLinkAttachError,
  isWebUrl,
} from "../mutations/use-card-attachment-writes";

function clientWith(api: Record<string, unknown>) {
  return createCardApi({ api } as unknown as NotrelixClient);
}

describe("cards.createCardLink (UC-COL-B07)", () => {
  it("sends only the name and address, with the caller's idempotency key", async () => {
    const post = vi.fn(async () => undefined);

    await clientWith({ post }).createCardLink(
      "item-1",
      { url: "https://example.com/a", name: "Spec" },
      { idempotencyKey: "key-1" },
    );

    expect(post).toHaveBeenCalledWith(
      "/board-items/item-1/attachments",
      { filename: "Spec", url: "https://example.com/a" },
      { idempotencyKey: "key-1" },
    );
  });

  it("lets the caller reuse one key to retry the same attempt", async () => {
    const post = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(undefined);
    const api = clientWith({ post });
    const link = { url: "https://example.com", name: "Spec" };

    await expect(
      api.createCardLink("item-1", link, { idempotencyKey: "k" }),
    ).rejects.toThrow("network");
    await api.createCardLink("item-1", link, { idempotencyKey: "k" });

    expect(post.mock.calls.map((call) => call[2])).toEqual([
      { idempotencyKey: "k" },
      { idempotencyKey: "k" },
    ]);
  });
});

describe("cards.getCardFiles", () => {
  it("drops an entry the server sent without an id", async () => {
    const get = vi.fn(async () => [
      { id: "att-1", filename: "a.pdf", kind: "File" },
      { filename: "no-id.pdf", kind: "File" },
    ]);

    const files = await clientWith({ get }).getCardFiles("item-1");

    expect(files.map((file) => file.id)).toEqual(["att-1"]);
  });
});

describe("mapAttachmentDtoToCardFile: the author", () => {
  it("leaves a missing author name empty, as the other mappers do", () => {
    const file = mapAttachmentDtoToCardFile(
      { id: "a", filename: "a.pdf", kind: "File", uploadedBy: "u-1" },
      "item-1",
    );

    expect(file.createdBy).toMatchObject({ userId: "u-1", name: "" });
  });
});

describe("classifyLinkAttachError", () => {
  it.each([
    [{ kind: "network" }, "offline"],
    [{ kind: "network", status: 0 }, "offline"],
    [{ status: 400 }, "rejected"],
    [{ status: 409 }, "limitReached"],
    [{ status: 403 }, "forbidden"],
    [{ status: 404 }, "notFound"],
    [{ status: 500 }, "failed"],
    [new Error("boom"), "failed"],
    [null, "failed"],
    ["text", "failed"],
  ])("maps %j to %s", (error, reason) => {
    expect(classifyLinkAttachError(error)).toBe(reason);
  });
});

describe("isWebUrl", () => {
  it.each([
    ["https://example.com/a?b=1", true],
    ["http://example.com", true],
    ["HTTPS://EXAMPLE.COM", true],
    ["javascript:alert(1)", false],
    ["data:text/html,x", false],
    ["ftp://example.com", false],
    ["/relative/path", false],
    ["example.com", false],
    ["", false],
  ])("%j is %s", (value, expected) => {
    expect(isWebUrl(value)).toBe(expected);
  });
});
