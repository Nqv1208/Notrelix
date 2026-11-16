import { afterEach, describe, expect, it, vi } from "vitest";
import { act, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { onSessionReset } from "@notrelix/query";
import { useInviteAccount } from "./use-invite-account";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const navigate = vi.fn();
const post = vi.fn(async () => undefined);
vi.mock("@tanstack/react-router", () => ({ useNavigate: () => navigate }));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({
    api: {
      api: { get: vi.fn(), post },
      endpoints: { auth: { logout: "/lo" } },
    },
  }),
}));
vi.mock("@notrelix/features-auth", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/features-auth")>()),
  useAuth: () => ({ user: { email: "khac@congty.vn" } }),
}));

const cleanups: Array<() => void> = [];
afterEach(() => cleanups.splice(0).forEach((run) => run()));

describe("switching account on an invitation", () => {
  it("empties the unsent stores, signs out and returns to the same invitation", async () => {
    const unsent = new Map([["card-1", "chưa gửi"]]);
    cleanups.push(onSessionReset(() => unsent.clear()));
    window.history.pushState({}, "", "/invite/tok");
    const current: { account?: ReturnType<typeof useInviteAccount> } = {};
    function Probe(): ReactNode {
      current.account = useInviteAccount();
      return null;
    }
    const root = createRoot(document.createElement("div"));
    cleanups.push(() => act(() => root.unmount()));
    await act(async () =>
      root.render(
        <QueryClientProvider client={new QueryClient()}>
          <Probe />
        </QueryClientProvider>,
      ),
    );
    expect(current.account!.signedInEmail).toBe("khac@congty.vn");
    await act(async () => current.account!.onSwitchAccount());
    expect(post).toHaveBeenCalledTimes(1);
    expect(unsent.size).toBe(0);
    expect(navigate).toHaveBeenCalledWith(
      expect.objectContaining({
        to: "/sign-in",
        search: { redirect: "/invite/tok" },
      }),
    );
  });
});
