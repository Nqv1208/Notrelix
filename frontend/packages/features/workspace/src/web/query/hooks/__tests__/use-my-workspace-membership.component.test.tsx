import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { renderHook, waitFor } from "@notrelix/testing";
import type { WorkspaceApiClient } from "../../../../core/api/workspace.service";
import { createUseMyWorkspaceMembership } from "../use-workspace-members";

function setup(get: (url: string) => Promise<unknown>) {
  const api = { get: vi.fn(get) } as unknown as WorkspaceApiClient;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <AccountScopeProvider accountId="acc-1">{children}</AccountScopeProvider>
    </QueryClientProvider>
  );
  const useMine = createUseMyWorkspaceMembership({ api });
  return { api, ...renderHook(() => useMine("ws-1"), { wrapper }) };
}

describe("my own membership read", () => {
  it("is loading, then carries my role and version from members/me", async () => {
    const { result, api } = setup(() =>
      Promise.resolve({
        userId: "u1",
        name: "Ha",
        role: "Guest",
        status: "Active",
        version: 4,
      }),
    );
    expect(result.current.isPending).toBe(true);

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toMatchObject({ role: "guest", version: 4 });
    expect(api.get).toHaveBeenCalledWith("/workspaces/ws-1/members/me");
  });

  it("is an error with no role when the producer says not found, so the shell offers nothing", async () => {
    const { result } = setup(() =>
      Promise.reject(Object.assign(new Error("not found"), { status: 404 })),
    );

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(result.current.data).toBeUndefined();
  });
});
