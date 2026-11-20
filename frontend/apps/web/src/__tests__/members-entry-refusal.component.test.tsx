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

const REFUSAL = {
  kind: "unknown",
  status: 400,
  details: {
    status: 400,
    errorCode: "business_rule.violation",
    ruleCode: "Workspaces_Invitation_EntryTargetNotActive",
    detail:
      "Every initial team or space must be an active team or space of this workspace.",
    entries: [
      { index: 0, kind: "team", targetId: "team-erp", reason: "not-active" },
    ],
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

describe("Invite with a Team entry the producer refuses", () => {
  it("FUI[workspace.members:invite-entry-refused] shows the refusal under the entry, not as a form error or a toast", async () => {
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
        throw REFUSAL;
      }),
      patch: vi.fn(),
      delete: vi.fn(),
    };
    mount();
    await flush();
    // The invite form lives in a dialog the "Invite people" button opens.
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
      const option = Array.from(
        document.body.querySelectorAll<HTMLElement>('[role="option"]'),
      ).find((candidate) => candidate.textContent?.includes("ERP Engineers"));
      expect(option, "the Team option").toBeDefined();
      option!.click();
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
    const alerts = Array.from(
      document.body.querySelectorAll('[role="alert"]'),
    ).map((node) => node.textContent);
    expect(alerts).toContain(
      "ERP Engineers isn't active any more, so it can't take this person. Remove it or choose another.",
    );
    expect(
      document.body.textContent,
      "the producer's one-line text is not shown",
    ).not.toContain("Every initial team or space must be");
    expect(h.toasts.filter((t) => t.startsWith("err:"))).toEqual([]);
  });
});
