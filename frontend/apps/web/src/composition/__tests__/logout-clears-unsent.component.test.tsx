import { describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createUseLogout } from "@notrelix/features-auth";
import { onSessionReset, runSessionReset } from "@notrelix/query";
import { renderHook, waitFor } from "@notrelix/testing";

function setup(post: () => Promise<void>) {
  const unsent = new Map([["card-1", "chưa gửi"]]);
  const off = onSessionReset(() => unsent.clear());
  const navigate = vi.fn();
  const useLogout = createUseLogout({
    api: { get: vi.fn(), post: post as never },
    endpoints: { auth: { logout: "/logout" } } as never,
    navigate,
    getSearchParams: () => new URLSearchParams(),
    onSignedOut: runSessionReset,
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={new QueryClient()}>
      {children}
    </QueryClientProvider>
  );
  return { unsent, off, navigate, ...renderHook(useLogout, { wrapper }) };
}

describe("sign-out", () => {
  it("empties the unsent stores after a successful logout", async () => {
    const s = setup(async () => undefined);
    s.result.current.mutate();
    await waitFor(() => expect(s.navigate).toHaveBeenCalled());
    expect(s.unsent.size).toBe(0);
    s.off();
  });

  it("keeps them when the logout failed and the session is still alive", async () => {
    const s = setup(() => Promise.reject(new Error("500")));
    s.result.current.mutate();
    await waitFor(() => expect(s.result.current.isError).toBe(true));
    expect(s.unsent.size).toBe(1);
    expect(s.navigate).not.toHaveBeenCalled();
    s.off();
  });
});
