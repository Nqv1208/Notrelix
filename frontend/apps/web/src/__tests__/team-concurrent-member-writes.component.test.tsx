import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { endpoints } from "@notrelix/contracts";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const h = vi.hoisted(() => ({
  api: null as unknown,
  toasts: [] as string[],
  surface: null as null | {
    onChangeRole: (userId: string, role: string) => void;
    onRemove: (userId: string) => void;
    onAddMember: (input: { userId: string; role: string }) => void;
    pendingUserIds: ReadonlySet<string>;
  },
}));

vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => ({ api: { api: h.api, endpoints } }),
}));
vi.mock("sonner", () => ({
  toast: {
    success: (m: string) => h.toasts.push(`ok:${m}`),
    error: (m: string) => h.toasts.push(`err:${m}`),
  },
}));
vi.mock("@tanstack/react-router", () => ({
  useParams: () => ({ teamId: "t1" }),
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({ workspaceId: "ws-1", workspace: null }),
}));
vi.mock("@/shell/workspace/use-workspace-viewer", () => ({
  useWorkspaceViewer: () => ({
    userId: "u-owner",
    role: "owner",
    members: { data: [] },
  }),
}));
vi.mock("@/shell/workspace/use-setup-frame", () => ({
  useSetupFrame: () => ({}),
}));
vi.mock("@/shell/workspace/use-shell-navigation", () => ({
  useShellNavigation: () => ({
    getHref: () => "",
    onNavigate: () => undefined,
  }),
}));
vi.mock("@/shell/workspace/use-teams-ext", () => ({
  useTeamsExt: () => null,
}));
vi.mock("@notrelix/features-workspace/web", async (original) => ({
  ...(await original<object>()),
  useWorkspaceTeamsCopy: () => ({
    toasts: new Proxy({}, { get: (_t, key) => String(key) }),
    roles: new Proxy({}, { get: (_t, key) => String(key) }),
    errors: new Proxy({}, { get: (_t, key) => String(key) }),
  }),
  WorkspaceTeamSurface: (props: NonNullable<typeof h.surface>) => {
    h.surface = props;
    return null;
  },
}));

import { TeamPage } from "../routes/workspaces/$workspaceId/teams.$teamId";

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
  root = host = null;
  h.toasts.length = 0;
  h.surface = null;
});

async function flush() {
  for (let i = 0; i < 20; i += 1)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 0));
    });
}

function mount(
  put: (url: string) => Promise<void>,
  del: (url: string) => Promise<void>,
) {
  h.api = {
    get: vi.fn(async (url: string) =>
      url.endsWith("/members")
        ? [
            { userId: "u1", displayName: "An", role: "Member" },
            { userId: "u2", displayName: "Binh", role: "Member" },
          ]
        : { id: "t1", name: "Helpdesk", status: "Active", version: 3 },
    ),
    put: vi.fn(put),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(del),
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
        <TeamPage />
      </QueryClientProvider>,
    );
  });
}

describe("Team page: writes on two members at once", () => {
  it("frees both members and reports both outcomes when two role changes overlap", async () => {
    const first = deferred();
    const second = deferred();
    mount(
      (url) => (url.includes("/u1") ? first.promise : second.promise),
      async () => undefined,
    );
    await flush();
    act(() => h.surface?.onChangeRole("u1", "Lead"));
    act(() => h.surface?.onChangeRole("u2", "Lead"));
    await flush();
    await act(async () => {
      first.resolve();
      second.resolve();
    });
    await flush();
    expect(
      [...(h.surface?.pendingUserIds ?? [])],
      "a member is still busy after its role change answered",
    ).toEqual([]);
    expect(h.toasts.filter((t) => t.startsWith("ok:"))).toHaveLength(2);
  });

  it("frees both members and reports both outcomes when two removals overlap", async () => {
    const first = deferred();
    const second = deferred();
    mount(
      async () => undefined,
      (url) => (url.includes("/u1") ? first.promise : second.promise),
    );
    await flush();
    act(() => h.surface?.onRemove("u1"));
    act(() => h.surface?.onRemove("u2"));
    await flush();
    await act(async () => {
      first.resolve();
      second.resolve();
    });
    await flush();
    expect([...(h.surface?.pendingUserIds ?? [])]).toEqual([]);
    expect(h.toasts.filter((t) => t.startsWith("ok:"))).toHaveLength(2);
  });

  it("shows the failure toast and frees the row when a role change is refused", async () => {
    mount(
      async () => {
        throw { status: 403, kind: "forbidden" };
      },
      async () => undefined,
    );
    await flush();
    act(() => h.surface?.onChangeRole("u1", "Lead"));
    await flush();
    expect(h.toasts.filter((t) => t.startsWith("err:"))).toHaveLength(1);
    expect(h.toasts.filter((t) => t.startsWith("ok:"))).toHaveLength(0);
    expect([...(h.surface?.pendingUserIds ?? [])]).toEqual([]);
  });

  it("shows the failure toast when an add is refused", async () => {
    mount(
      async () => undefined,
      async () => undefined,
    );
    (h.api as { post: ReturnType<typeof vi.fn> }).post.mockRejectedValue({
      status: 409,
      kind: "conflict",
    });
    await flush();
    act(() => h.surface?.onAddMember({ userId: "u3", role: "Member" }));
    await flush();
    expect(h.toasts.filter((t) => t.startsWith("err:"))).toHaveLength(1);
    expect(h.toasts.filter((t) => t.startsWith("ok:"))).toHaveLength(0);
  });
});
