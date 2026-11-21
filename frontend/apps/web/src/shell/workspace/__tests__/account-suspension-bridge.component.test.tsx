import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { workspaceQueryKeys } from "@notrelix/features-workspace";

const h = vi.hoisted(() => {
  const listeners = new Set<() => void>();
  return {
    signal: {
      notify: () => listeners.forEach((listener) => listener()),
      subscribe: (listener: () => void) => {
        listeners.add(listener);
        return () => listeners.delete(listener);
      },
    },
  };
});

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ accountSuspension: h.signal }),
}));
vi.mock("sonner", () => ({ toast: { error: vi.fn() } }));

import { toast } from "sonner";
import { AccountSuspensionBridge } from "../account-suspension-bridge";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | null = null;
let host: HTMLDivElement | null = null;
let client: QueryClient;

beforeEach(async () => {
  vi.mocked(toast.error).mockReset();
  client = new QueryClient();
  client.setQueryData(workspaceQueryKeys.detail("w1"), {
    id: "w1",
    accountStatus: "Active",
  });
  client.setQueryData(workspaceQueryKeys.detail("w2"), { id: "w2" });
  client.setQueryData(workspaceQueryKeys.members("w1"), [{ id: "m" }]);
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  await act(async () => {
    root!.render(
      <QueryClientProvider client={client}>
        <I18nProvider catalogs={[]} initialLocale="en">
          <AccountSuspensionBridge />
        </I18nProvider>
      </QueryClientProvider>,
    );
  });
});
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
});

describe("AccountSuspensionBridge (D06)", () => {
  it("FUI[account.suspended:refused-writes] says it once for several refused writes and flips the cached workspaces to read-only without a request", () => {
    act(() => {
      h.signal.notify();
      h.signal.notify();
      h.signal.notify();
    });
    const calls = vi.mocked(toast.error).mock.calls;
    expect(calls.length).toBeGreaterThan(0);
    expect(new Set(calls.map((call) => call[1]?.id)).size).toBe(1);
    expect(calls[0]![0]).toBe(
      "The Account is suspended. Changes are paused; you can still read.",
    );
    expect(client.getQueryData(workspaceQueryKeys.detail("w1"))).toMatchObject({
      accountStatus: "Suspended",
    });
    expect(client.getQueryData(workspaceQueryKeys.detail("w2"))).toMatchObject({
      accountStatus: "Suspended",
    });
    // Nothing else in the cache is touched.
    expect(client.getQueryData(workspaceQueryKeys.members("w1"))).toEqual([
      { id: "m" },
    ]);
  });

  it("keeps a Closed Account Closed", () => {
    client.setQueryData(workspaceQueryKeys.detail("w1"), {
      id: "w1",
      accountStatus: "Closed",
    });
    act(() => h.signal.notify());
    expect(client.getQueryData(workspaceQueryKeys.detail("w1"))).toMatchObject({
      accountStatus: "Closed",
    });
  });
});
