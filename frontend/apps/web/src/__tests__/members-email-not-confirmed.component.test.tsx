import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AccountScopeProvider } from "@notrelix/query";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { endpoints } from "@notrelix/contracts";
import { webCatalogs } from "../i18n/catalogs";
import { registerEmailConfirmationRecovery } from "../shell/workspace/email-confirmation-recovery";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const h = vi.hoisted(() => ({
  api: null as unknown,
  toasts: [] as string[],
  errorOptions: [] as {
    description?: string;
    action?: { label: string; onClick: () => void };
  }[],
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({
  toast: {
    success: (message: string) => h.toasts.push(`ok:${message}`),
    error: (message: string, options?: (typeof h.errorOptions)[number]) => {
      h.toasts.push(`err:${message}`);
      h.errorOptions.push(options ?? {});
    },
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

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  host = null;
  h.toasts.length = 0;
  h.errorOptions.length = 0;
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

const UNCONFIRMED = {
  kind: "forbidden",
  status: 403,
  details: {
    status: 403,
    errorCode: "auth.email_not_confirmed",
    detail: "Email must be confirmed before using this feature.",
  },
};

function setText(el: HTMLTextAreaElement, value: string) {
  const setter = Object.getOwnPropertyDescriptor(
    HTMLTextAreaElement.prototype,
    "value",
  )!.set!;
  setter.call(el, value);
  el.dispatchEvent(new Event("input", { bubbles: true }));
}

describe("Invite with a Team entry while the inviter's email is not confirmed", () => {
  it("says to confirm the email and offers to resend the link, not that the Team cannot be granted", async () => {
    const resend = vi.fn();
    const unregister = registerEmailConfirmationRecovery({
      description: "Confirm your email first, then try again.",
      actionLabel: "Resend email",
      resend,
    });
    h.api = {
      get: vi.fn(async (url: string) => {
        if (url.includes("/teams"))
          return {
            items: [
              {
                id: "team-erp",
                name: "ERP Engineers",
                status: "Active",
                version: 1,
              },
            ],
            nextCursor: null,
          };
        return url.includes("/invitations")
          ? { items: [], nextCursor: null }
          : [];
      }),
      post: vi.fn(async () => {
        throw UNCONFIRMED;
      }),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    mount();
    await flush();
    await act(async () =>
      Array.from(document.body.querySelectorAll("button"))
        .find((candidate) => candidate.textContent?.trim() === "Invite people")
        ?.click(),
    );
    const picker = document.body.querySelector<HTMLElement>(
      '[aria-label="Choose a Team or Space to add"]',
    );
    expect(picker, "the entry picker").not.toBeNull();
    await act(async () => {
      picker!.dispatchEvent(
        new KeyboardEvent("keydown", { key: "Enter", bubbles: true }),
      );
    });
    await act(async () => {
      Array.from(document.body.querySelectorAll<HTMLElement>('[role="option"]'))
        .find((candidate) => candidate.textContent?.includes("ERP Engineers"))
        ?.click();
    });
    await act(async () => {
      setText(document.body.querySelector("textarea")!, "an.do@example.com");
    });
    const send = Array.from(document.body.querySelectorAll("button")).find(
      (candidate) => candidate.textContent?.trim() === "Send invitation",
    );
    await act(async () => send?.click());
    await flush();

    expect((h.api as { post: unknown }).post).toHaveBeenCalledTimes(1);
    expect(h.errorOptions).toHaveLength(1);
    expect(h.errorOptions[0]!.description).toBe(
      "Confirm your email first, then try again.",
    );
    expect(h.errorOptions[0]!.action?.label).toBe("Resend email");
    h.errorOptions[0]!.action!.onClick();
    expect(resend).toHaveBeenCalledOnce();
    expect(document.body.textContent).not.toContain("can't take this person");
    unregister();
  });
});
