import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import {
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";

const state = vi.hoisted(() => ({
  navigated: [] as unknown[],
  navigateProps: [] as unknown[],
  replaced: [] as string[],
  searchStr: "",
}));

vi.mock("@tanstack/react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-router")>()),
  useLocation: () => ({ searchStr: state.searchStr }),
  useNavigate: () => (options: unknown) => void state.navigated.push(options),
  useRouter: () => ({
    history: { replace: (to: string) => void state.replaced.push(to) },
  }),
  Navigate: (props: unknown) => {
    state.navigateProps.push(props);
    return null;
  },
}));
vi.mock("@notrelix/features-auth", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/features-auth")>()),
  useAuth: () => ({ isAuthenticated: true, isLoading: false }),
  SignInFlow: () => null,
}));
vi.mock("../routes/auth-flow-deps", () => ({
  useAuthFlowDeps: () => ({ service: {}, navigation: {} }),
}));
vi.mock("../routes/auth-layout", () => ({
  AuthLayout: ({ children }: { children: React.ReactNode }) => children,
}));

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

// Each test re-imports its route after vi.resetModules (sign-in keeps module state),
// so the first cold transform of the router and auth packages is paid here, once, with
// its own budget, rather than inside a 5s test timeout on a loaded runner.
beforeAll(async () => {
  await import("../routes/sign-in");
  await import("../routes/auth-callback");
  await import("../routes/index");
}, 60_000);

function reset() {
  state.navigated = [];
  state.navigateProps = [];
  state.replaced = [];
  state.searchStr = "";
  window.history.pushState({}, "", "/");
}

beforeEach(reset);

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = undefined;
  host = undefined;
  reset();
});

async function mount(element: React.ReactElement) {
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => root?.render(element));
}

describe("Where a sign-in lands", { timeout: 30_000 }, () => {
  it("opens the default workspace first when a signed-in person opens the sign-in address with no return address", async () => {
    window.history.pushState({}, "", "/sign-in");
    vi.resetModules();
    const { SignInPage } = await import("../routes/sign-in");
    await mount(<SignInPage />);
    expect(state.navigated).toEqual([
      { to: "/home", search: { open: "default" }, replace: true },
    ]);
  });

  it("goes back to the return address, not the default workspace, when one came along", async () => {
    window.history.pushState({}, "", "/sign-in");
    state.searchStr = "?returnTo=/workspaces/w-3/boards/b-1";
    vi.resetModules();
    const { SignInPage } = await import("../routes/sign-in");
    await mount(<SignInPage />);
    expect(state.navigated).toEqual([
      { to: "/workspaces/w-3/boards/b-1", replace: true },
    ]);
  });

  it("opens the default workspace first after a provider sign-in with no return address", async () => {
    vi.resetModules();
    const { AuthCallbackPage } = await import("../routes/auth-callback");
    await mount(<AuthCallbackPage />);
    expect(state.replaced).toEqual(["/home?open=default"]);
  });

  it("goes back to the return address after a provider sign-in that carried one", async () => {
    window.history.pushState(
      {},
      "",
      "/auth/callback?returnTo=/account/profile",
    );
    vi.resetModules();
    const { AuthCallbackPage } = await import("../routes/auth-callback");
    await mount(<AuthCallbackPage />);
    expect(state.replaced).toEqual(["/account/profile"]);
  });

  it("opens the default workspace first from the root address", async () => {
    vi.resetModules();
    const { IndexPage } = await import("../routes/index");
    await mount(<IndexPage />);
    expect(state.navigateProps).toEqual([
      { to: "/home", search: { open: "default" }, replace: true },
    ]);
  });
});
