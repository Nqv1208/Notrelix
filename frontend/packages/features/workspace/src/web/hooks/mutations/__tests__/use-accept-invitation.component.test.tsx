import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { endpoints } from "@notrelix/contracts";
import type { WorkspaceApiClient } from "../../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../../query/keys";
import { createUseAcceptInvitation } from "../use-accept-invitation";

function setup(post: () => Promise<unknown>) {
  const queryClient = new QueryClient({
    defaultOptions: { mutations: { retry: false } },
  });
  const invalidate = vi.spyOn(queryClient, "invalidateQueries");
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <AccountScopeProvider accountId="acc-1">{children}</AccountScopeProvider>
    </QueryClientProvider>
  );
  const useAccept = createUseAcceptInvitation({
    api: { post } as unknown as WorkspaceApiClient,
    endpoints,
  });
  return { invalidate, ...renderHook(() => useAccept(), { wrapper }) };
}

const invalidatedKeys = (invalidate: {
  mock: { calls: readonly (readonly unknown[])[] };
}) =>
  invalidate.mock.calls.map(
    ([filters]) => (filters as { queryKey: unknown }).queryKey,
  );

describe("Accepting an invitation by id", () => {
  it("reads the pending list and the workspaces again after an accepted answer", async () => {
    const { invalidate, result } = setup(() =>
      Promise.resolve({ workspaceId: "w1", workspaceSlug: "acme" }),
    );
    await act(() => result.current.mutateAsync("inv-1"));
    expect(invalidatedKeys(invalidate)).toEqual([
      workspaceQueryKeys.pendingInvitations("acc-1"),
      workspaceQueryKeys.all("acc-1"),
    ]);
  });

  it("reads the workspaces again when the answer is refused, because a lost reply may have admitted the person", async () => {
    const gone = Object.assign(new Error("not found"), { status: 404 });
    const { invalidate, result } = setup(() => Promise.reject(gone));
    await act(async () => {
      await result.current.mutateAsync("inv-1").catch(() => undefined);
    });
    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(invalidatedKeys(invalidate)).toContainEqual(
      workspaceQueryKeys.all("acc-1"),
    );
  });
});
