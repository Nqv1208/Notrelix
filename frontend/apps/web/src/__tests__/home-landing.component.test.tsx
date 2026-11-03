import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { webCatalogs } from "../i18n/catalogs";

const state = vi.hoisted(() => ({
  workspaces: [] as { id: string }[],
  account: {} as Record<string, unknown>,
  navigated: [] as string[],
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: {}, endpoints: {} } }),
}));
vi.mock("@notrelix/features-workspace/web", async (importOriginal) => ({
  ...(await importOriginal<
    typeof import("@notrelix/features-workspace/web")
  >()),
  createUseWorkspaceList: () => () => ({
    data: state.workspaces,
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  }),
}));
vi.mock("@notrelix/features-account", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/features-account")>()),
  createUseAccountWorkspaces: () => () => state.account,
}));
vi.mock("@tanstack/react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-router")>()),
  useNavigate: () => vi.fn(),
  Navigate: ({ to }: { to: string }) => {
    state.navigated.push(to);
    return null;
  },
}));
vi.mock("@/shell/guards/auth-guard", () => ({
  AuthGuard: ({ children }: { children: React.ReactNode }) => children,
}));
vi.mock("@/shell/workspace/create-workspace-dialog", () => ({
  CreateWorkspaceDialog: () => null,
}));

import { HomePage } from "../routes/home";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  state.workspaces = [];
  state.account = {};
  state.navigated = [];
});

async function mount() {
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(
      <I18nProvider catalogs={webCatalogs} initialLocale="en">
        <HomePage />
      </I18nProvider>,
    );
  });
}

describe("Home landing of a person with no workspace", () => {
  it("sends them to Your workspaces when a workspace of theirs is in Recently deleted", async () => {
    state.account = { workspaces: [], deleted: [{ id: "w-9" }] };
    await mount();
    expect(state.navigated).toEqual(["/account/workspaces"]);
  });

  it("sends them to onboarding when nothing is waiting to be restored", async () => {
    state.account = { workspaces: [], deleted: [] };
    await mount();
    expect(state.navigated).toEqual(["/onboarding"]);
  });

  it("waits for the Recently deleted read before choosing, and falls through when it fails", async () => {
    state.account = { workspaces: [] };
    await mount();
    expect(state.navigated).toEqual([]);
    state.account = { workspaces: [], deletedIsError: true };
    await mount();
    expect(state.navigated).toEqual(["/onboarding"]);
  });

  it("still opens a workspace first when the person has one", async () => {
    state.workspaces = [{ id: "w-1" }];
    state.account = { workspaces: [], deleted: [{ id: "w-9" }] };
    await mount();
    expect(state.navigated).toEqual(["/workspaces/$workspaceId"]);
  });
});
