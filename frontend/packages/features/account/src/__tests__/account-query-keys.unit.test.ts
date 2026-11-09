import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { accountQueryKeys } from "../query/keys";

describe("accountQueryKeys Q7 purity tests", () => {
  it("every accountQueryKey satisfies assertNotrelixQueryKey", () => {
    const keys = [
      accountQueryKeys.all("acc-1"),
      accountQueryKeys.profile("acc-1"),
      accountQueryKeys.preferences("acc-1"),
      accountQueryKeys.security("acc-1"),
      accountQueryKeys.sessions("acc-1"),
      accountQueryKeys.activity("acc-1"),
      accountQueryKeys.workspaces("acc-1"),
      accountQueryKeys.deletedWorkspaces("acc-1"),
      accountQueryKeys.memberProfile("acc-1", "ws-1"),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("account");
      expect(key[1]).toBe("acc-1");
      expect(key[2]).toBe("account");
    }
  });

  it("nests every resource under the Account root, so a reset of the Account drops them all", () => {
    const root = accountQueryKeys.all("acc-1");
    const resources = [
      accountQueryKeys.profile("acc-1"),
      accountQueryKeys.sessions("acc-1"),
      accountQueryKeys.workspaces("acc-1"),
      accountQueryKeys.deletedWorkspaces("acc-1"),
      accountQueryKeys.memberProfile("acc-1", "ws-1"),
    ];

    for (const key of resources) {
      expect(key.slice(0, root.length)).toEqual(root);
    }
  });

  it("keeps two Accounts disjoint for the same resource", () => {
    expect(accountQueryKeys.workspaces("acc-a")).not.toEqual(
      accountQueryKeys.workspaces("acc-b"),
    );
    expect(accountQueryKeys.memberProfile("acc-a", "ws-1")).not.toEqual(
      accountQueryKeys.memberProfile("acc-b", "ws-1"),
    );
  });
});
