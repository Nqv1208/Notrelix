import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const state = vi.hoisted(() => ({
  navigated: [] as unknown[],
  mockApi: false,
  get: vi.fn(),
}));

vi.mock("@tanstack/react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-router")>()),
  useNavigate: () => (options: unknown) => void state.navigated.push(options),
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({
    api: { api: { get: state.get, post: vi.fn() } },
    env: { mockApi: state.mockApi, apiUrl: "https://api.example" },
  }),
}));

import { useAuthFlowDeps } from "../routes/auth-flow-deps";

/** The navigation the hook builds, read from a mounted probe. */
function navigationOf() {
  let navigation: ReturnType<typeof useAuthFlowDeps>["navigation"] | undefined;
  function Probe() {
    navigation = useAuthFlowDeps().navigation;
    return null;
  }
  const host = document.createElement("div");
  const root = createRoot(host);
  act(() => root.render(<Probe />));
  act(() => root.unmount());
  return navigation!;
}

afterEach(() => {
  state.navigated = [];
  state.get.mockReset();
  vi.restoreAllMocks();
});

describe("company sign-in navigation (web host)", () => {
  it("leaves for the provider's https address", () => {
    const assign = vi.fn();
    vi.stubGlobal("location", { ...window.location, assign });
    state.mockApi = false;
    navigationOf().startSsoSignIn("https://login.example/authorize?state=s");
    expect(assign).toHaveBeenCalledWith(
      "https://login.example/authorize?state=s",
    );
    vi.unstubAllGlobals();
  });

  it.each(["javascript:alert(1)", "http://login.example/a", "not a url"])(
    "never navigates to %s: it lands on the sign-in page with a plain failure",
    (address) => {
      const assign = vi.fn();
      vi.stubGlobal("location", { ...window.location, assign });
      state.mockApi = false;
      navigationOf().startSsoSignIn(address);
      expect(assign).not.toHaveBeenCalled();
      expect(state.navigated).toEqual([
        {
          to: "/sign-in",
          search: { error: "identity.sso.failed" },
          replace: true,
        },
      ]);
      vi.unstubAllGlobals();
    },
  );

  it("in the mock world plays the callback with the state and lands where the redirect would", async () => {
    state.mockApi = true;
    state.get.mockResolvedValue({
      redirectTo: "/auth/callback?returnTo=%2Fhome",
    });
    navigationOf().startSsoSignIn(
      "https://mock-idp.invalid/authorize?state=st-1&code=c-9",
    );
    await vi.waitFor(() => expect(state.navigated).toHaveLength(1));
    expect(state.get).toHaveBeenCalledWith(
      "/auth/sso/callback?code=c-9&state=st-1",
      expect.objectContaining({ skipAuthRefresh: true }),
    );
    expect(state.navigated[0]).toEqual({
      to: "/auth/callback",
      search: { returnTo: "/home" },
      replace: true,
    });
  });
});
