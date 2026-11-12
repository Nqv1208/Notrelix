import { describe, expect, it } from "vitest";
import {
  CHAT_LINK_PREVIEWS_MAX,
  findProductLinks,
  linkRefKey,
} from "../core/model/link-model";

const WS = "ws-1";

describe("findProductLinks (UC-CHT-G05)", () => {
  it("finds a Page link and a Board item link, relative or on any host", () => {
    const links = findProductLinks(
      `read /workspaces/${WS}/docs/page-1 and https://app.test/workspaces/${WS}/boards/b-1?view=table&item=card-9 now`,
      WS,
    );

    expect(links.map((link) => link.ref)).toEqual([
      { kind: "documents.page", resourceId: "page-1" },
      { kind: "work-management.board-item", resourceId: "card-9" },
    ]);
    expect(links[0]!.href).toBe(`/workspaces/${WS}/docs/page-1`);
    expect(links[1]!.href).toBe(
      `/workspaces/${WS}/boards/b-1?view=table&item=card-9`,
    );
  });

  it("leaves out another Workspace, a Board without an item and other addresses", () => {
    expect(
      findProductLinks(
        `/workspaces/other/docs/p-1 /workspaces/${WS}/boards/b-1 /workspaces/${WS}/spaces/s-1 https://example.test/x`,
        WS,
      ),
    ).toEqual([]);
  });

  it("drops a trailing sentence mark, shows a resource once and stops at the limit", () => {
    expect(
      findProductLinks(`see /workspaces/${WS}/docs/p-1.`, WS)[0]!.resourceId,
    ).toBe("p-1");
    expect(
      findProductLinks(
        `/workspaces/${WS}/docs/p-1 /workspaces/${WS}/docs/p-1`,
        WS,
      ),
    ).toHaveLength(1);
    const many = Array.from(
      { length: 6 },
      (_, i) => `/workspaces/${WS}/docs/p-${i}`,
    ).join(" ");
    expect(findProductLinks(many, WS)).toHaveLength(CHAT_LINK_PREVIEWS_MAX);
  });

  it("reads a long run of marks and of host characters in linear time", () => {
    const started = Date.now();
    findProductLinks(`/workspaces/${WS}/docs/p-1${"!".repeat(50_000)}`, WS);
    findProductLinks(`https://${"a".repeat(50_000)}`, WS);
    expect(Date.now() - started).toBeLessThan(1000);
  });

  it("names a link to the resolver by kind and resource", () => {
    expect(linkRefKey({ kind: "documents.page", resourceId: "p-1" })).toBe(
      "documents.page:p-1",
    );
    expect(linkRefKey({ url: "/x" })).toBe("url:/x");
  });
});
