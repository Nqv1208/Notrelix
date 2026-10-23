import { describe, expect, it } from "vitest";
import { createElement } from "react";
import { renderToString } from "react-dom/server";
import { QueryClient } from "@tanstack/react-query";
import {
  AccountScopeProvider,
  accountQueryKey,
  resetCacheForAccountTransition,
  useAccountId,
} from "../index";

function Probe() {
  return createElement("span", null, useAccountId());
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

describe("AccountScopeProvider / useAccountId", () => {
  it("ACC-001 returns the active Account ID", () => {
    const html = renderToString(
      createElement(
        AccountScopeProvider,
        { accountId: "acc-a" },
        createElement(Probe),
      ),
    );
    expect(html).toContain("acc-a");
  });

  it("ACC-002 fails loudly without an active Account", () => {
    expect(() =>
      renderToString(
        createElement(
          AccountScopeProvider,
          { accountId: null },
          createElement(Probe),
        ),
      ),
    ).toThrow(/active Account/);
    expect(() => renderToString(createElement(Probe))).toThrow(
      /active Account/,
    );
  });
});

describe("Account transition isolation", () => {
  const profileKey = (id: string) => accountQueryKey(id, "profile");

  it("ACC-003 A -> B -> A: B reads nothing of A, switching back refetches", async () => {
    const client = new QueryClient();
    let fetches = 0;
    const load = (id: string) =>
      client.fetchQuery({
        queryKey: profileKey(id),
        queryFn: async () => ({ id, n: ++fetches }),
      });

    await load("acc-a");
    expect(client.getQueryData(profileKey("acc-a"))).toEqual({
      id: "acc-a",
      n: 1,
    });

    // Switch to B: key scoping alone already hides A's data.
    expect(client.getQueryData(profileKey("acc-b"))).toBeUndefined();
    resetCacheForAccountTransition(client);
    expect(client.getQueryCache().getAll()).toHaveLength(0);
    expect(client.getQueryData(profileKey("acc-a"))).toBeUndefined();

    expect(await load("acc-b")).toEqual({ id: "acc-b", n: 2 });

    // Back to A: previous A data is gone, so it refetches.
    resetCacheForAccountTransition(client);
    expect(await load("acc-a")).toEqual({ id: "acc-a", n: 3 });
    expect(client.getQueryData(profileKey("acc-b"))).toBeUndefined();
  });

  it("ACC-004 key scoping alone keeps A and B disjoint without a reset", async () => {
    const client = new QueryClient();
    client.setQueryData(profileKey("acc-a"), "A");
    expect(client.getQueryData(profileKey("acc-b"))).toBeUndefined();
    client.setQueryData(profileKey("acc-b"), "B");
    expect(client.getQueryData(profileKey("acc-a"))).toBe("A");
  });

  it("ACC-005 late response from the previous Account does not write into the cache", async () => {
    const client = new QueryClient();
    const late = deferred<string>();
    const pending = client
      .fetchQuery({
        queryKey: profileKey("acc-a"),
        queryFn: () => late.promise,
        retry: false,
      })
      .catch(() => undefined);

    resetCacheForAccountTransition(client);
    client.setQueryData(profileKey("acc-b"), "B");

    late.resolve("A-late");
    await pending;

    expect(client.getQueryData(profileKey("acc-a"))).toBeUndefined();
    expect(client.getQueryData(profileKey("acc-b"))).toBe("B");
    expect(client.getQueryCache().getAll()).toHaveLength(1);
  });
});
