import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@notrelix/i18n";
import { webCatalogs } from "../i18n/catalogs";

const state = vi.hoisted(() => ({
  account: {} as Record<string, unknown>,
  navigated: [] as string[],
  routed: [] as string[],
  setDefault: [] as string[],
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: {}, endpoints: {} } }),
}));
vi.mock("@notrelix/features-account", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/features-account")>()),
  createUseAccountWorkspaces: () => () => state.account,
  createUseProfile: () => () => ({ profile: { name: "An" } }),
}));
vi.mock("@notrelix/query", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/query")>()),
  useAccountId: () => "a-1",
}));
vi.mock("@tanstack/react-query", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-query")>()),
  useQueryClient: () => ({ invalidateQueries: async () => undefined }),
}));
vi.mock("@tanstack/react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@tanstack/react-router")>()),
  useRouter: () => ({
    navigate: ({ to }: { to: string }) => {
      state.routed.push(to);
    },
  }),
  Navigate: ({ to }: { to: string }) => {
    state.navigated.push(to);
    return null;
  },
}));
vi.mock("@/shell/guards/auth-guard", () => ({
  AuthGuard: ({ children }: { children: React.ReactNode }) => children,
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
  state.account = {};
  state.navigated = [];
  state.routed = [];
  state.setDefault = [];
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

const workspace = (id: string, name: string, isDefault = false) => ({
  id,
  name,
  role: "owner",
  memberCount: 3,
  departmentCount: 1,
  isDefault,
  canLeave: true,
  isArchived: false,
});

describe("Home overview", () => {
  it("lists the workspaces and opens one only when its card is chosen", async () => {
    state.account = {
      workspaces: [
        workspace("w-1", "Minh An"),
        workspace("w-2", "Studio", true),
      ],
      deleted: [],
      setDefault: async (id: string) => void state.setDefault.push(id),
    };
    await mount();
    expect(state.navigated).toEqual([]);
    expect(host?.querySelector("h1")?.textContent).toBe("Hello, An");
    const open = host?.querySelector<HTMLAnchorElement>(
      'a[aria-label="Open Studio"]',
    );
    expect(open?.getAttribute("href")).toBe("/workspaces/w-2");
    await act(async () => open?.click());
    expect(state.routed).toEqual(["/workspaces/w-2"]);
  });

  it("marks the default and offers to make another one the default", async () => {
    state.account = {
      workspaces: [
        workspace("w-1", "Minh An"),
        workspace("w-2", "Studio", true),
      ],
      deleted: [],
      setDefault: async (id: string) => void state.setDefault.push(id),
    };
    await mount();
    expect(host?.textContent).toContain("Opens by default");
    const make = host?.querySelector<HTMLButtonElement>(
      'button[aria-label^="Make Minh An"]',
    );
    await act(async () => make?.click());
    expect(state.setDefault).toEqual(["w-1"]);
  });

  it("shows Recently deleted instead of onboarding when something can be restored", async () => {
    state.account = {
      workspaces: [],
      deleted: [
        {
          id: "w-9",
          name: "Old",
          deletedAt: "2026-11-01T00:00:00Z",
          purgeAfter: "2026-12-01T00:00:00Z",
          version: 1,
        },
      ],
    };
    await mount();
    expect(state.navigated).toEqual([]);
    expect(host?.textContent).toContain("Recently deleted");
  });

  it("sends a person with nothing to open or restore to onboarding", async () => {
    state.account = { workspaces: [], deleted: [] };
    await mount();
    expect(state.navigated).toEqual(["/onboarding"]);
  });

  it("waits for the Recently deleted read, and falls through when it fails", async () => {
    state.account = { workspaces: [] };
    await mount();
    expect(state.navigated).toEqual([]);
    state.account = { workspaces: [], deletedIsError: true };
    await mount();
    expect(state.navigated).toEqual(["/onboarding"]);
  });
});
