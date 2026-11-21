import { describe, expect, it } from "vitest";
import { resolveWorkspaceScope } from "../shell/workspace/workspace-scope";

const base = {
  workspaceId: "ws-1",
  list: { isLoading: false, isError: false, ids: ["ws-2"] as string[] },
  detail: {
    isLoading: false,
    isError: false,
    hasData: true,
    errorKind: undefined,
    isArchived: true,
  },
};

describe("resolveWorkspaceScope for an archived workspace (I05)", () => {
  it("opens an archived workspace the member's list leaves out, because the record answered for them", () => {
    expect(resolveWorkspaceScope(base)).toBeNull();
  });

  it("still reads a workspace missing from the list as lost access when it is not archived", () => {
    expect(
      resolveWorkspaceScope({
        ...base,
        detail: { ...base.detail, isArchived: false },
      }),
    ).toBe("forbidden");
  });

  it("answers forbidden, the same as a made-up id, when the record is refused", () => {
    expect(
      resolveWorkspaceScope({
        ...base,
        detail: {
          isLoading: false,
          isError: true,
          hasData: false,
          errorKind: "not_found",
          isArchived: false,
        },
      }),
    ).toBe("forbidden");
  });

  it("waits for the record before deciding when the list leaves the workspace out", () => {
    expect(
      resolveWorkspaceScope({
        ...base,
        detail: {
          isLoading: true,
          isError: false,
          hasData: false,
          errorKind: undefined,
          isArchived: false,
        },
      }),
    ).toBe("loading");
  });
});
