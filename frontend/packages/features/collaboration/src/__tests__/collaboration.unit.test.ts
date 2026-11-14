import { describe, it, expect } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { collaborationQueryKeys } from "../core/query/keys";

describe("collaborationQueryKeys", () => {
  it("should generate resource comments key scoped to the workspace", () => {
    expect(collaborationQueryKeys.comments("ws-1", "page-1")).toEqual([
      "collaboration",
      "comments",
      "ws-1",
      "page-1",
    ]);
  });

  it("should generate reactions key", () => {
    expect(collaborationQueryKeys.reactions("page-1")).toEqual([
      "collaboration",
      "reactions",
      "page-1",
    ]);
  });
});

describe("collaboration comment cache across a Workspace switch (UC-COL-G15)", () => {
  it("keeps the same target's comments in separate entries per Workspace", () => {
    const client = new QueryClient();
    client.setQueryData(collaborationQueryKeys.comments("ws-1", "item-9"), [
      { id: "c-1", body: "from workspace one" },
    ]);

    expect(
      client.getQueryData(collaborationQueryKeys.comments("ws-2", "item-9")),
    ).toBeUndefined();
    expect(
      client.getQueryData(collaborationQueryKeys.comments("ws-1", "item-9")),
    ).toEqual([{ id: "c-1", body: "from workspace one" }]);
  });

  it("a late response for the previous Workspace is stored under its own key, not the current one", () => {
    const client = new QueryClient();
    const current = collaborationQueryKeys.comments("ws-2", "item-9");

    // The response for ws-1 arrives after the switch to ws-2.
    client.setQueryData(collaborationQueryKeys.comments("ws-1", "item-9"), [
      { id: "stale", body: "old workspace" },
    ]);

    expect(client.getQueryData(current)).toBeUndefined();
  });
});
