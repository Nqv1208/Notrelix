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
  toasts: [] as string[],
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
  useNavigate: () => () => undefined,
  useSearch: () => ({}),
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { name: "Acme", memberCount: 3 },
  }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => {
  const rows = [
    {
      id: "u-owner",
      userId: "u-owner",
      name: "Owen Owner",
      role: "owner",
      membership: "active",
      version: 1,
    },
    {
      id: "u-alan",
      userId: "u-alan",
      name: "Alan Turing",
      role: "member",
      membership: "active",
      version: 3,
    },
    {
      id: "u-sam",
      userId: "u-sam",
      name: "Sam Okafor",
      role: "member",
      membership: "suspended",
      version: 5,
    },
  ];
  const query = {
    data: rows,
    isError: false,
    isSuccess: true,
    isPending: false,
    refetch: () => Promise.resolve(),
  };
  return {
    useWorkspaceViewer: () => ({
      userId: "u-owner",
      role: "owner",
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
vi.mock("../routes/workspaces/$workspaceId/billing/seat-invite", () => ({
  SeatInviteDialog: () => null,
}));
vi.mock("@notrelix/features-workspace/web", async (original) => ({
  ...(await original<object>()),
  WorkspaceSetupFrame: ({ children }: { children: unknown }) => children,
}));

import { MembersPage } from "../routes/workspaces/$workspaceId/members";

function deferred() {
  let resolve!: () => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<void>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
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
              <MembersPage />
            </ThemeProvider>
          </I18nProvider>
        </AccountScopeProvider>
      </QueryClientProvider>,
    );
  });
}

function api(post: (url: string, body: unknown) => Promise<unknown>) {
  h.api = {
    get: vi.fn(async () => []),
    post: vi.fn(post),
    patch: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  };
  return (h.api as { post: ReturnType<typeof vi.fn> }).post;
}

const byLabel = (label: string) =>
  document.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);

const byText = (selector: string, text: string) =>
  Array.from(document.querySelectorAll<HTMLElement>(selector)).find(
    (el) => el.textContent?.trim() === text,
  ) ?? null;

async function openRowAction(name: string, action: string) {
  const trigger = byLabel(`Actions for ${name}`);
  if (!trigger) throw new Error(`No actions button for ${name}`);
  await act(async () => {
    trigger.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Enter", bubbles: true }),
    );
  });
  const item = byText('[role="menuitem"]', action);
  if (!item) throw new Error(`No menu item ${action}`);
  await act(async () => item.click());
}

async function confirmSuspend() {
  const dialog = document.querySelector('[role="alertdialog"]');
  const confirm = Array.from(
    dialog?.querySelectorAll<HTMLButtonElement>("button") ?? [],
  ).find((button) => button.textContent === "Suspend access");
  if (!confirm) throw new Error("No confirm button");
  await act(async () => confirm.click());
}

describe("Members: suspend and activate through the producer", () => {
  it("asks first, names the consequence and the workspace, then suspends with the member's version", async () => {
    const post = api(async () => undefined);
    mount();
    await flush();

    await openRowAction("Alan Turing", "Suspend access");
    const dialog = document.querySelector('[role="alertdialog"]');
    expect(dialog?.textContent).toContain("Suspend Alan Turing?");
    expect(dialog?.textContent).toContain(
      "Alan Turing can't open Acme until an admin reactivates access. Nothing is deleted.",
    );
    expect(post, "nothing is sent before the confirmation").not.toHaveBeenCalled();

    await confirmSuspend();
    await flush();
    expect(post).toHaveBeenCalledTimes(1);
    expect(post).toHaveBeenCalledWith(
      "/workspaces/ws-1/members/u-alan/suspend",
      { expectedVersion: 3 },
    );
    expect(h.toasts).toEqual(["ok:Alan Turing's access is suspended."]);
  });

  it("shows the row as busy while the suspension is in flight, then frees it", async () => {
    const pending = deferred();
    api(() => pending.promise);
    mount();
    await flush();

    await openRowAction("Alan Turing", "Suspend access");
    await confirmSuspend();
    await flush();
    expect(
      document.body.textContent,
      "the row says the write is running",
    ).toContain("Updating role for Alan Turing…");
    expect(byLabel("Actions for Alan Turing")).toBeNull();

    await act(async () => pending.resolve());
    await flush();
    expect(byLabel("Actions for Alan Turing")).not.toBeNull();
  });

  it("says nothing was suspended when another admin changed the member first (stale version, 412)", async () => {
    api(async () => {
      throw Object.assign(new Error("stale"), { status: 412 });
    });
    mount();
    await flush();

    await openRowAction("Alan Turing", "Suspend access");
    await confirmSuspend();
    await flush();
    expect(h.toasts).toEqual([
      "err:Alan Turing's access wasn't suspended.|Someone else changed this just now. The list was refreshed; review it and try again.",
    ]);
  });

  it("tells a person without the right to suspend to ask an owner (403)", async () => {
    api(async () => {
      throw Object.assign(new Error("denied"), { status: 403 });
    });
    mount();
    await flush();

    await openRowAction("Alan Turing", "Suspend access");
    await confirmSuspend();
    await flush();
    expect(h.toasts).toEqual([
      "err:Alan Turing's access wasn't suspended.|You don't have permission to do this. Ask a workspace owner.",
    ]);
  });

  it("reactivates a suspended member at once with the member's version", async () => {
    const post = api(async () => undefined);
    mount();
    await flush();

    await openRowAction("Sam Okafor", "Reactivate access");
    await flush();
    expect(post).toHaveBeenCalledWith(
      "/workspaces/ws-1/members/u-sam/activate",
      { expectedVersion: 5 },
    );
    expect(h.toasts).toEqual(["ok:Sam Okafor's access is active again."]);
  });

  it("shows the producer's seat refusal as the reason when no seat is free, and offers no upgrade path here", async () => {
    api(async () => {
      throw Object.assign(new Error("rule"), {
        status: 400,
        details: {
          errorCode: "business_rule.violation",
          detail: "Your plan has no free seat for this member.",
        },
      });
    });
    mount();
    await flush();

    await openRowAction("Sam Okafor", "Reactivate access");
    await flush();
    expect(h.toasts).toEqual([
      "err:Sam Okafor's access wasn't reactivated.|Your plan has no free seat for this member.",
    ]);
  });

  it("says nothing was reactivated on a stale version (412)", async () => {
    api(async () => {
      throw Object.assign(new Error("stale"), { status: 412 });
    });
    mount();
    await flush();

    await openRowAction("Sam Okafor", "Reactivate access");
    await flush();
    expect(h.toasts).toEqual([
      "err:Sam Okafor's access wasn't reactivated.|Someone else changed this just now. The list was refreshed; review it and try again.",
    ]);
  });
});
