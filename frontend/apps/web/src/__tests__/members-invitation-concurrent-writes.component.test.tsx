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
    error: (message: string) => h.toasts.push(`err:${message}`),
  },
}));
vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => () => undefined,
  useSearch: () => ({ tab: "invitations" }),
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { name: "Acme", memberCount: 1 },
  }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => {
  const query = {
    data: [
      {
        id: "u-owner",
        userId: "u-owner",
        name: "Owner",
        role: "owner",
        membership: "active",
        version: 1,
      },
    ],
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

const FUTURE = new Date(Date.now() + 7 * 86_400_000).toISOString();
const row = (id: string, email: string) => ({
  id,
  email,
  role: "Member",
  status: "Pending",
  expiresAt: FUTURE,
  createdAt: new Date().toISOString(),
  isAccepted: false,
  version: 1,
});

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
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

const button = (label: string) =>
  host?.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`) ??
  null;

describe("Invitations tab: writes on two rows at once", () => {
  it("frees both rows and reports both outcomes when two resends overlap", async () => {
    const first = deferred();
    const second = deferred();
    h.api = {
      get: vi.fn(async (url: string) =>
        url.includes("/invitations")
          ? {
              items: [row("i-a", "a@x.test"), row("i-b", "b@x.test")],
              nextCursor: null,
            }
          : [],
      ),
      post: vi.fn((url: string) =>
        url.includes("/i-a/resend") ? first.promise : second.promise,
      ),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    mount();
    await flush();

    act(() => button("Resend invitation to a@x.test")?.click());
    act(() => button("Resend invitation to b@x.test")?.click());
    await flush();
    expect((h.api as { post: unknown }).post).toHaveBeenCalledTimes(2);
    await act(async () => {
      first.resolve();
      second.resolve();
    });
    await flush();

    expect(
      button("Resend invitation to a@x.test"),
      "row a is still busy after its resend answered",
    ).not.toBeNull();
    expect(h.toasts.filter((t) => t.startsWith("ok:")).length).toBe(2);
  });
});
