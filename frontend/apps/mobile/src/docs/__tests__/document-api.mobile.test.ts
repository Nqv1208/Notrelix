import { describe, expect, it, vi } from "vitest";
import {
  DocumentRequestError,
  createDocumentApi,
  isNoAccess,
} from "../document-api";
import { createDocumentLinkHandlers } from "../document-links";
import { documentQueryKey } from "../use-document";

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response;
}

const PAGE = { id: "p1", workspaceId: "w1", title: "Plan" };
const BLOCKS = [{ id: "b1", pageId: "p1", type: "Text", version: 1 }];

describe("mobile document read", () => {
  it("reads the page and its blocks with the session cookie", async () => {
    const fetchImpl = vi.fn(async (url: string, _init?: RequestInit) =>
      jsonResponse(url.endsWith("/blocks") ? BLOCKS : PAGE),
    );
    const api = createDocumentApi({
      baseUrl: "https://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    const read = await api.read("w1", "p1");
    expect(read.page.title).toBe("Plan");
    expect(read.blocks).toHaveLength(1);
    const urls = fetchImpl.mock.calls.map((call) => call[0]);
    expect(urls).toEqual([
      "https://api.test/pages/p1",
      "https://api.test/pages/p1/blocks",
    ]);
    expect(fetchImpl.mock.calls[0]?.[1]).toMatchObject({
      method: "GET",
      credentials: "include",
    });
  });

  it("answers no access for a page of another workspace", async () => {
    const fetchImpl = vi.fn(async (url: string) =>
      jsonResponse(
        url.endsWith("/blocks") ? [] : { ...PAGE, workspaceId: "other" },
      ),
    );
    const api = createDocumentApi({
      baseUrl: "https://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    await expect(api.read("w1", "p1")).rejects.toSatisfy(isNoAccess);
  });

  it.each([401, 403, 404])("treats %i as no access", async (status) => {
    const fetchImpl = vi.fn(async () => jsonResponse({}, status));
    const api = createDocumentApi({
      baseUrl: "https://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    await expect(api.read("w1", "p1")).rejects.toSatisfy(isNoAccess);
  });

  it("does not treat a server failure as no access", async () => {
    const fetchImpl = vi.fn(async () => jsonResponse({}, 500));
    const api = createDocumentApi({
      baseUrl: "https://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    const error = await api.read("w1", "p1").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(DocumentRequestError);
    expect(isNoAccess(error)).toBe(false);
  });

  it("scopes the query key by workspace", () => {
    expect(documentQueryKey("w1", "p1")).not.toEqual(
      documentQueryKey("w2", "p1"),
    );
  });
});

describe("mobile document links", () => {
  it("opens only web, mail and phone links", () => {
    const openUrl = vi.fn();
    const links = createDocumentLinkHandlers({
      workspaceId: "w1",
      openUrl,
      push: vi.fn(),
    });
    links.onOpenLink("https://example.com");
    links.onOpenLink("mailto:a@b.c");
    links.onOpenLink("javascript:alert(1)");
    links.onOpenLink("file:///etc/passwd");
    expect(openUrl.mock.calls.map((c) => c[0])).toEqual([
      "https://example.com",
      "mailto:a@b.c",
    ]);
  });

  it("opens a referenced page inside the same workspace", () => {
    const push = vi.fn();
    createDocumentLinkHandlers({
      workspaceId: "w1",
      openUrl: vi.fn(),
      push,
    }).onOpenPage("p 2");
    expect(push).toHaveBeenCalledWith("/workspaces/w1/docs/p%202");
  });
});
