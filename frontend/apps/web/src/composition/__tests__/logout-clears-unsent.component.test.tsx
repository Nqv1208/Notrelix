import { afterEach, describe, expect, it, vi } from "vitest";
import { act, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createUseLogout } from "@notrelix/features-auth";
import { onSessionReset, runSessionReset } from "@notrelix/query";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

type Logout = ReturnType<ReturnType<typeof createUseLogout>>;
const cleanups: Array<() => void> = [];
afterEach(() => cleanups.splice(0).forEach((run) => run()));

async function mountLogout(post: () => Promise<void>) {
  const unsent = new Map([["card-1", "chưa gửi"]]);
  cleanups.push(onSessionReset(() => unsent.clear()));
  const navigate = vi.fn();
  const useLogout = createUseLogout({
    api: { get: vi.fn(), post: post as never },
    endpoints: { auth: { logout: "/logout" } } as never,
    navigate,
    getSearchParams: () => new URLSearchParams(),
    onSignedOut: runSessionReset,
  });
  const current: { logout?: Logout } = {};
  function Probe(): ReactNode {
    current.logout = useLogout();
    return null;
  }
  const root: Root = createRoot(document.createElement("div"));
  cleanups.push(() => act(() => root.unmount()));
  await act(async () =>
    root.render(
      <QueryClientProvider client={new QueryClient()}>
        <Probe />
      </QueryClientProvider>,
    ),
  );
  return { unsent, navigate, current };
}

describe("sign-out", () => {
  it("empties the unsent stores after a successful logout", async () => {
    const s = await mountLogout(async () => undefined);
    await act(async () => s.current.logout!.mutate());
    expect(s.navigate).toHaveBeenCalled();
    expect(s.unsent.size).toBe(0);
  });

  it("keeps them when the logout failed and the session is still alive", async () => {
    const s = await mountLogout(() => Promise.reject(new Error("500")));
    await act(async () => s.current.logout!.mutate());
    expect(s.unsent.size).toBe(1);
    expect(s.navigate).not.toHaveBeenCalled();
  });
});
