import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { docsQueryKeys } from "../query/keys";

describe("docsQueryKeys Q7 purity tests", () => {
  it("every docsQueryKey satisfies assertNotrelixQueryKey and contains workspaceId", () => {
    const wsId = "ws-123";
    const pageId = "page-456";

    const keys = [
      docsQueryKeys.all(wsId),
      docsQueryKeys.tree(wsId),
      docsQueryKeys.list(wsId),
      docsQueryKeys.detail(wsId, pageId),
      docsQueryKeys.breadcrumb(wsId, pageId),
      docsQueryKeys.blocks(wsId, pageId),
      docsQueryKeys.comments(wsId, pageId),
      docsQueryKeys.history(wsId, pageId),
      docsQueryKeys.search(wsId, "test"),
      docsQueryKeys.favorites(wsId),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("workspace");
      expect(key[1]).toBe(wsId);
      expect(key[2]).toBe("documents");
    }
  });
});
