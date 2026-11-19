import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { endpoints } from "@notrelix/contracts";
import { webCatalogs } from "../i18n/catalogs";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const h = vi.hoisted(() => ({
  api: null as unknown,
  role: "owner" as string,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));
vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => vi.fn(async () => undefined),
  useSearch: () => ({}),
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { id: "ws-1", name: "Acme", version: 7, memberCount: 3 },
    isLoading: false,
    isError: false,
    refetch: () => Promise.resolve(),
  }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => {
  const query = {
    data: [],
    isError: false,
    isSuccess: true,
    isPending: false,
    refetch: () => Promise.resolve(),
  };
  return {
    useWorkspaceViewer: () => ({
      userId: "u-viewer",
      role: h.role,
      mine: query,
      canListMembers: true,
      members: query,
      can: () => true,
    }),
  };
});
vi.mock("@/shell/workspace/use-setup-frame", () => ({
  useSetupFrame: () => ({
    workspaceId: "ws-1",
    workspaceName: "Acme",
    active: "general",
    counts: {},
    links: {},
  }),
}));
vi.mock("@/shell/workspace/use-teams-ext", () => ({
  useSecurityExt: () => ({
    data: {
      guestCount: 2,
      guestExpiryDays: null,
      companyDomain: null,
      linksOffSince: null,
    },
    isError: false,
    refetch: () => undefined,
  }),
}));
vi.mock("@notrelix/features-workspace/web", async (original) => {
  const actual = await original<Record<string, unknown>>();
  return {
    ...actual,
    WorkspaceSetupFrame: ({ children }: { children: unknown }) => children,
  };
});

import { SettingsPage } from "../routes/workspaces/$workspaceId/settings";
import { SecurityPage } from "../routes/workspaces/$workspaceId/security";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  document.body.innerHTML = "";
  root = null;
  host = null;
  h.role = "owner";
});

async function flush() {
  for (let i = 0; i < 20; i += 1)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
}

async function mount(page: "settings" | "security", role: string) {
  h.role = role;
  const put = vi.fn(async () => ({}));
  h.api = {
    get: vi.fn(async () => ({})),
    post: vi.fn(),
    patch: vi.fn(),
    put,
    delete: vi.fn(),
  };
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  act(() => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <AccountScopeProvider accountId="acc-test">
          <I18nProvider catalogs={webCatalogs} initialLocale="en">
            <ThemeProvider target={null}>
              {page === "settings" ? <SettingsPage /> : <SecurityPage />}
            </ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
  await flush();
  return put;
}

const buttons = () =>
  Array.from(document.body.querySelectorAll<HTMLButtonElement>("button"));

describe("Workspace settings are Owner-only (ManageWorkspace)", () => {
  it("settings: an Admin sees the form read-only, with no enabled field, no save and no danger zone", async () => {
    await mount("settings", "admin");
    const name = document.querySelector<HTMLInputElement>("input");
    expect(name, "the profile form is still drawn").not.toBeNull();
    expect(name?.disabled || name?.readOnly).toBe(true);
    expect(document.body.textContent, "the read-only notice shows").toContain(
      "You can see them here",
    );
    expect(
      buttons().some((el) => el.textContent?.trim() === "Archive workspace"),
    ).toBe(false);
    for (const save of buttons().filter(
      (el) => el.textContent?.trim() === "Save changes",
    ))
      expect(save.disabled).toBe(true);
  });

  it("settings: an Owner still edits the form and sees the danger zone", async () => {
    await mount("settings", "owner");
    const name = document.querySelector<HTMLInputElement>("input");
    expect(name?.disabled).toBe(false);
    expect(name?.readOnly).toBe(false);
    expect(
      buttons().some((el) => el.textContent?.trim() === "Archive workspace"),
    ).toBe(true);
  });

  it("sharing: an Admin cannot flip the links switch and nothing is written", async () => {
    const put = await mount("security", "admin");
    const toggle = document.querySelector<HTMLButtonElement>('[role="switch"]');
    expect(toggle?.getAttribute("aria-disabled")).toBe("true");
    await act(async () => toggle?.click());
    await flush();
    expect(put).not.toHaveBeenCalled();
  });

  it("sharing: an Owner flips the links switch and the write goes out", async () => {
    const put = await mount("security", "owner");
    const toggle = document.querySelector<HTMLButtonElement>('[role="switch"]');
    expect(toggle?.getAttribute("aria-disabled")).toBeNull();
    await act(async () => toggle?.click());
    await flush();
    expect(put).toHaveBeenCalledTimes(1);
  });
});
