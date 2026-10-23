import { describe, expect, it, vi } from "vitest";
import { accountQueryKey, createQueryClient } from "@notrelix/query";
import { createSessionEventBus } from "@notrelix/runtime-web";
import { createApplicationLifecycle } from "../application-lifecycle";

function createHarness() {
  const queryClient = createQueryClient();
  const realtime = {
    connect: vi.fn(async () => undefined),
    disconnect: vi.fn(),
    subscribe: vi.fn(() => () => undefined),
    subscribeState: vi.fn(() => () => undefined),
    subscribeRecovery: vi.fn(() => () => undefined),
    getState: vi.fn(() => "connected" as const),
    dispose: vi.fn(),
  };
  const lifecycle = createApplicationLifecycle({
    queryClient,
    realtime,
    sessionEvents: createSessionEventBus(),
    navigateToSignedOut: vi.fn(),
  });
  return { queryClient, realtime, lifecycle };
}

const key = (accountId: string) => accountQueryKey(accountId, "profile");

describe("application lifecycle — account (principal) isolation", () => {
  it("ACCT-001 first principal initialization does not clear the cache", () => {
    const { queryClient, lifecycle } = createHarness();
    queryClient.setQueryData(key("acc-a"), "A");

    lifecycle.prepareAccountTransition("acc-a");

    expect(queryClient.getQueryData(key("acc-a"))).toBe("A");
    lifecycle.dispose();
  });

  it("ACCT-002 A -> B -> A: B reads nothing from A, back to A refetches", async () => {
    const { queryClient, realtime, lifecycle } = createHarness();
    let fetches = 0;
    const load = (id: string) =>
      queryClient.fetchQuery({
        queryKey: key(id),
        queryFn: async () => ({ id, n: ++fetches }),
      });

    lifecycle.prepareAccountTransition("acc-a");
    await load("acc-a");

    lifecycle.prepareAccountTransition("acc-b");
    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    expect(queryClient.getQueryData(key("acc-a"))).toBeUndefined();
    expect(realtime.disconnect).toHaveBeenCalledWith("account-switch");
    expect(await load("acc-b")).toEqual({ id: "acc-b", n: 2 });

    lifecycle.prepareAccountTransition("acc-a");
    expect(queryClient.getQueryData(key("acc-b"))).toBeUndefined();
    expect(await load("acc-a")).toEqual({ id: "acc-a", n: 3 });
    lifecycle.dispose();
  });

  it("ACCT-003 a late response from the previous principal never lands in the cache", async () => {
    const { queryClient, lifecycle } = createHarness();
    let release!: (value: string) => void;
    const late = new Promise<string>((resolve) => {
      release = resolve;
    });

    lifecycle.prepareAccountTransition("acc-a");
    const pending = queryClient
      .fetchQuery({ queryKey: key("acc-a"), queryFn: () => late, retry: false })
      .catch(() => undefined);

    lifecycle.prepareAccountTransition("acc-b");
    queryClient.setQueryData(key("acc-b"), "B");
    release("A-late");
    await pending;

    expect(queryClient.getQueryData(key("acc-a"))).toBeUndefined();
    expect(queryClient.getQueryData(key("acc-b"))).toBe("B");
    lifecycle.dispose();
  });

  it("ACCT-004 same principal and sign-out do not clear the cache", () => {
    const { queryClient, realtime, lifecycle } = createHarness();
    lifecycle.prepareAccountTransition("acc-a");
    queryClient.setQueryData(key("acc-a"), "A");

    lifecycle.prepareAccountTransition("acc-a");
    lifecycle.prepareAccountTransition(null);

    expect(queryClient.getQueryData(key("acc-a"))).toBe("A");
    expect(realtime.disconnect).not.toHaveBeenCalled();
    lifecycle.dispose();
  });

  it("ACCT-005 sign-out then a different principal still resets the cache", () => {
    const { queryClient, lifecycle } = createHarness();
    lifecycle.prepareAccountTransition("acc-a");
    queryClient.setQueryData(key("acc-a"), "A");

    lifecycle.prepareAccountTransition(null);
    lifecycle.prepareAccountTransition("acc-b");

    expect(queryClient.getQueryCache().getAll()).toHaveLength(0);
    lifecycle.dispose();
  });
});
