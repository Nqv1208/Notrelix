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
  toasts: [] as string[],
  navigate: null as unknown as ReturnType<typeof import("vitest").vi.fn>,
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({
  toast: {
    success: (message: string) => h.toasts.push(`ok:${message}`),
    error: (message: string, options?: { description?: string }) =>
      h.toasts.push(`err:${message}|${options?.description ?? ""}`),
  },
}));
vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => h.navigate,
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
  useSetupFrame: () => ({}),
}));
vi.mock("@notrelix/features-workspace/web", async (original) => ({
  ...(await original<object>()),
  WorkspaceSetupFrame: ({ children }: { children: unknown }) => children,
}));

import { SettingsPage } from "../routes/workspaces/$workspaceId/settings";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>((res) => {
    resolve = res;
  });
  return { promise, resolve };
}

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  document.body.innerHTML = "";
  root = null;
  host = null;
  h.toasts.length = 0;
  h.role = "owner";
});

async function flush() {
  for (let i = 0; i < 20; i += 1)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
}

function mount() {
  h.navigate = vi.fn(async () => undefined);
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
              <SettingsPage />
            </ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
}

function api(post: (url: string, body: unknown) => Promise<unknown>) {
  h.api = {
    get: vi.fn(async () => ({})),
    post: vi.fn(post),
    patch: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  };
  return (h.api as { post: ReturnType<typeof vi.fn> }).post;
}

const button = (root: ParentNode, text: string) =>
  Array.from(root.querySelectorAll<HTMLButtonElement>("button")).find(
    (el) => el.textContent?.trim() === text,
  ) ?? null;

async function openArchiveDialog() {
  const trigger = button(document.body, "Archive workspace");
  if (!trigger) throw new Error("No archive button");
  await act(async () => trigger.click());
  const dialog = document.querySelector<HTMLElement>('[role="alertdialog"]');
  if (!dialog) throw new Error("No confirmation dialog");
  return dialog;
}

async function typeName(dialog: HTMLElement, value: string) {
  const input = dialog.querySelector<HTMLInputElement>("input");
  if (!input) throw new Error("No name input");
  await act(async () => {
    Object.getOwnPropertyDescriptor(
      HTMLInputElement.prototype,
      "value",
    )?.set?.call(input, value);
    input.dispatchEvent(new Event("input", { bubbles: true }));
  });
}

async function archiveAs(name: string) {
  const dialog = await openArchiveDialog();
  await typeName(dialog, name);
  const confirm = button(dialog, "Archive workspace");
  if (!confirm) throw new Error("No confirm button");
  await act(async () => confirm.click());
  await flush();
}

describe("Workspace settings: archive through the producer", () => {
  it("asks first and keeps confirm off until the exact workspace name is typed", async () => {
    const post = api(async () => undefined);
    mount();
    await flush();

    const dialog = await openArchiveDialog();
    expect(dialog.textContent).toContain("Archive Acme?");
    expect(button(dialog, "Archive workspace")?.disabled).toBe(true);

    await typeName(dialog, "acme");
    expect(button(dialog, "Archive workspace")?.disabled).toBe(true);
    expect(dialog.textContent).toContain("Type Acme exactly to continue.");

    await typeName(dialog, "Acme");
    expect(button(dialog, "Archive workspace")?.disabled).toBe(false);
    expect(
      post,
      "nothing is sent before the confirmation",
    ).not.toHaveBeenCalled();
  });

  it("archives with the workspace version, says so, and leaves for home", async () => {
    const post = api(async () => undefined);
    mount();
    await flush();

    await archiveAs("Acme");
    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith("/workspaces/ws-1/archive", {
      expectedVersion: 7,
    });
    expect(h.toasts).toEqual(["ok:Acme was archived."]);
    expect(h.navigate).toHaveBeenCalledWith({ to: "/home" });
  });

  it("shows the action as running and offers no second write while the archive is in flight", async () => {
    const pending = deferred();
    const post = api(() => pending.promise);
    mount();
    await flush();

    await archiveAs("Acme");
    const running = button(document.body, "Archiving…");
    expect(running, "the button says the write is running").not.toBeNull();
    expect(running?.disabled).toBe(true);
    await act(async () => running?.click());
    expect(post).toHaveBeenCalledTimes(1);
    expect(h.toasts).toEqual([]);

    await act(async () => pending.resolve());
    await flush();
    expect(h.toasts).toEqual(["ok:Acme was archived."]);
  });

  it("says the workspace changed when the version is stale (412)", async () => {
    api(async () => {
      throw Object.assign(new Error("stale"), { status: 412 });
    });
    mount();
    await flush();

    await archiveAs("Acme");
    expect(h.toasts).toEqual([
      "err:Acme wasn't archived.|Acme changed since you opened this page. Reload and try again.",
    ]);
    expect(h.navigate).not.toHaveBeenCalled();
  });

  it("tells a person without the right to archive that they lack permission (403)", async () => {
    api(async () => {
      throw Object.assign(new Error("denied"), { status: 403 });
    });
    mount();
    await flush();

    await archiveAs("Acme");
    expect(h.toasts).toEqual([
      "err:Acme wasn't archived.|You don't have permission to change these settings.",
    ]);
    expect(h.navigate).not.toHaveBeenCalled();
  });

  it("offers a plain member no danger zone and no archive action", async () => {
    api(async () => undefined);
    h.role = "member";
    mount();
    await flush();

    expect(button(document.body, "Archive workspace")).toBeNull();
    expect(document.body.textContent).not.toContain("Danger zone");
  });
});
