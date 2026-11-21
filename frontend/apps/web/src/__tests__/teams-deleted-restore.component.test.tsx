import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
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
  toasts: [] as { kind: "ok" | "err"; message: string; description?: string }[],
  role: "admin" as string,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({
  toast: {
    success: (message: string) => h.toasts.push({ kind: "ok", message }),
    error: (message: string, options?: { description?: string }) =>
      h.toasts.push({
        kind: "err",
        message,
        description: options?.description,
      }),
  },
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { name: "Acme", memberCount: 2 },
  }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => ({
  useWorkspaceViewer: () => ({
    role: h.role,
    userId: "u-admin",
    can: () => true,
  }),
}));
vi.mock("@/shell/workspace/use-setup-frame", () => ({
  useSetupFrame: () => ({
    workspaceId: "ws-1",
    workspaceName: "Acme",
    active: "teams",
    counts: { members: 2, teams: 1, departments: 0 },
    links: {},
  }),
}));
vi.mock("@/shell/workspace/use-teams-ext", () => ({ useTeamsExt: () => null }));
vi.mock("@/shell/workspace/use-shell-navigation", () => ({
  useShellNavigation: () => ({
    getHref: () => "#",
    onNavigate: () => undefined,
  }),
}));

import { TeamsPage } from "../routes/workspaces/$workspaceId/teams";

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
  h.toasts.length = 0;
  h.role = "admin";
});

async function flush() {
  for (let i = 0; i < 20; i += 1)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
}

function mount() {
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
              <TeamsPage />
            </ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
}

const DELETED = {
  id: "team-old",
  name: "Old project",
  description: null,
  memberCount: 3,
  deletedAt: "2026-11-02T09:00:00Z",
  deletedBy: "u-admin",
  deletedByName: "Ada Admin",
};

/** A producer holding `deleted`: a restore moves a Team out of it. */
function producer(deleted: (typeof DELETED)[], onRestore?: () => never) {
  const post = vi.fn(async (url: string) => {
    if (onRestore) return onRestore();
    if (url.endsWith("/restore")) deleted.splice(0, deleted.length);
    return undefined;
  });
  const get = vi.fn(async (url: string) => {
    if (url.includes("/teams/deleted")) return [...deleted];
    if (url.includes("/teams")) return { items: [], nextCursor: null };
    return [];
  });
  h.api = { get, post, patch: vi.fn(), delete: vi.fn(), put: vi.fn() };
  return { get, post };
}

const click = async (label: string | RegExp) => {
  const target = Array.from(host!.querySelectorAll("button")).find(
    (candidate) =>
      typeof label === "string"
        ? candidate.getAttribute("aria-label") === label ||
          candidate.textContent?.trim() === label
        : label.test(candidate.textContent ?? ""),
  );
  expect(target, `the "${String(label)}" button`).toBeDefined();
  await act(async () => target!.click());
};

describe("Teams page: restore a deleted Team", () => {
  it("FUI[workspace.teams:deleted-restore] reads the deleted Teams, restores one and shows the list empty after", async () => {
    const { get, post } = producer([{ ...DELETED }]);
    mount();
    await flush();

    expect(
      get.mock.calls.some(([url]) => String(url).includes("/teams/deleted")),
      "an Owner or Admin reads the deleted Teams",
    ).toBe(true);
    await click(/^Deleted/);
    expect(host?.textContent).toContain("Old project");
    expect(host?.textContent).toContain("Ada Admin");

    await click("Restore Old project");
    await flush();

    expect(post).toHaveBeenCalledTimes(1);
    expect(String(post.mock.calls[0]![0])).toContain("/teams/team-old/restore");
    expect(h.toasts).toEqual([
      { kind: "ok", message: "Old project was restored." },
    ]);
    expect(host?.textContent).toContain("No deleted Teams");
  });

  it("FUI[workspace.teams:deleted-restore-failed] a refused restore says so and keeps the Team in the list", async () => {
    producer([{ ...DELETED }], () => {
      throw { kind: "forbidden", status: 403, details: { status: 403 } };
    });
    mount();
    await flush();

    await click(/^Deleted/);
    await click("Restore Old project");
    await flush();

    expect(h.toasts.filter((t) => t.kind === "ok")).toEqual([]);
    expect(h.toasts.filter((t) => t.kind === "err")).toHaveLength(1);
    expect(host?.textContent).toContain("Old project");
  });

  it("FUI[workspace.teams:deleted-member] a Member never asks for the deleted Teams", async () => {
    h.role = "member";
    const { get } = producer([{ ...DELETED }]);
    mount();
    await flush();

    expect(
      get.mock.calls.some(([url]) => String(url).includes("/teams/deleted")),
      "no read of the deleted Teams for someone who may not restore",
    ).toBe(false);
    expect(host?.textContent).not.toContain("Old project");
  });
});
