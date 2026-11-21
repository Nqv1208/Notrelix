import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor, within } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { en } from "../web/components/chat.copy";
import { collapseKey } from "../web/components/use-collapsed-groups";
import { ChatWorkspace } from "../web/components/chat-workspace";

const WORKSPACE = mockIds.workspaces.primary;

type ViewerRole = "owner" | "admin" | "member" | "guest";

function mount(
  viewerRole: ViewerRole,
  options: {
    persona?: MockPersona;
    collapseStore?: {
      getItem(k: string): string | null;
      setItem(k: string, v: string): void;
    };
    fetchOverride?: (url: string, method: string) => Promise<Response> | null;
  } = {},
) {
  const store = new MockStore({
    ...defaultConfig,
    persona: options.persona ?? "owner",
  });
  const mockFetch = createMockFetch(store);
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => {
      const url = typeof input === "string" ? input : String(input);
      const method = init?.method ?? "GET";
      return options.fetchOverride?.(url, method) ?? mockFetch(input, init);
    },
  });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
        {children}
      </I18nProvider>
    </QueryClientProvider>
  );
  const view = render(
    <ChatWorkspace
      deps={{ api: client.api, endpoints: client.endpoints }}
      workspaceId={WORKSPACE}
      viewerId={store.getCurrentUser().id}
      viewerRole={viewerRole}
      channelId={null}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={new Map()}
      collapseStore={options.collapseStore}
    />,
    { wrapper },
  );
  const team = [...store.chat.groups.values()].find((g) => g.name === "Team")!;
  return { store, team, ...view };
}

const THIRTY = 30_000;

describe("Sidebar categories in the workspace (UC-CHT-A02)", () => {
  it(
    "creates a category from the sidebar header: it shows as an empty heading",
    async () => {
      const { store } = mount("owner");
      fireEvent.click(
        await screen.findByRole("button", { name: en.groups.newCategory }),
      );
      fireEvent.change(await screen.findByLabelText(en.groups.nameLabel), {
        target: { value: "Operations" },
      });
      fireEvent.click(screen.getByRole("button", { name: en.groups.create }));

      await waitFor(() =>
        expect(
          [...store.chat.groups.values()].some((g) => g.name === "Operations"),
        ).toBe(true),
      );
      expect(await screen.findByText("Operations")).not.toBeNull();
      await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
      expect(screen.getAllByText(en.groups.emptyManage).length).toBeGreaterThan(
        0,
      );
    },
    THIRTY,
  );

  it(
    "shows a duplicate name inline, keeps the dialog open and stores nothing",
    async () => {
      const { store } = mount("owner");
      const before = store.chat.groups.size;
      fireEvent.click(
        await screen.findByRole("button", { name: en.groups.newCategory }),
      );
      fireEvent.change(await screen.findByLabelText(en.groups.nameLabel), {
        target: { value: "Team" },
      });
      fireEvent.click(screen.getByRole("button", { name: en.groups.create }));

      expect((await screen.findByRole("alert")).textContent).toBe(
        en.groups.nameTaken,
      );
      expect(store.chat.groups.size).toBe(before);
      expect(screen.getByLabelText(en.groups.nameLabel)).not.toBeNull();
    },
    THIRTY,
  );

  it(
    "reads a dropped connection as offline and stores nothing",
    async () => {
      const { store } = mount("owner", {
        fetchOverride: (url, method) =>
          method === "POST" && url.includes("/chat/channel-groups")
            ? Promise.reject(new TypeError("Failed to fetch"))
            : null,
      });
      const before = store.chat.groups.size;
      fireEvent.click(
        await screen.findByRole("button", { name: en.groups.newCategory }),
      );
      fireEvent.change(await screen.findByLabelText(en.groups.nameLabel), {
        target: { value: "Offline one" },
      });
      fireEvent.click(screen.getByRole("button", { name: en.groups.create }));

      expect((await screen.findByRole("alert")).textContent).toBe(
        en.feedback.network,
      );
      expect(store.chat.groups.size).toBe(before);
    },
    THIRTY,
  );

  it(
    "reads a server 403 as permission denied when the client offered the control",
    async () => {
      // The client thinks the viewer is an Admin; the server knows a Member.
      mount("admin", { persona: "member" });
      fireEvent.click(
        await screen.findByRole("button", { name: en.groups.newCategory }),
      );
      fireEvent.change(await screen.findByLabelText(en.groups.nameLabel), {
        target: { value: "Nope" },
      });
      fireEvent.click(screen.getByRole("button", { name: en.groups.create }));
      expect((await screen.findByRole("alert")).textContent).toBe(
        en.groups.forbidden,
      );
    },
    THIRTY,
  );

  it(
    "offers no category control to a Member, or to a Guest",
    async () => {
      const { unmount } = mount("member", { persona: "member" });
      await screen.findByRole("navigation", { name: en.sidebar.navLabel });
      await screen.findByText("Team");
      expect(
        screen.queryByRole("button", { name: en.groups.newCategory }),
      ).toBeNull();
      expect(
        screen.queryByRole("button", { name: /Options for category/ }),
      ).toBeNull();
      unmount();

      mount("guest", { persona: "member" });
      await screen.findByRole("navigation", { name: en.sidebar.navLabel });
      expect(
        screen.queryByRole("button", { name: en.groups.newCategory }),
      ).toBeNull();
    },
    THIRTY,
  );

  it(
    "renames a category from its menu",
    async () => {
      const { store, team } = mount("owner");
      fireEvent.keyDown(
        await screen.findByRole("button", {
          name: "Options for category Team",
        }),
        { key: "Enter" },
      );
      fireEvent.click(
        await screen.findByRole("menuitem", { name: en.groups.rename }),
      );
      const input = (await screen.findByLabelText(
        en.groups.nameLabel,
      )) as HTMLInputElement;
      expect(input.value).toBe("Team");
      fireEvent.change(input, { target: { value: "Crew" } });
      fireEvent.click(screen.getByRole("button", { name: en.groups.save }));

      await waitFor(() =>
        expect(store.chat.groups.get(team.id)?.name).toBe("Crew"),
      );
      expect(await screen.findByText("Crew")).not.toBeNull();
    },
    THIRTY,
  );

  it(
    "a stale rename closes the dialog and shows the version another person saved",
    async () => {
      const { store, team } = mount("owner");
      fireEvent.keyDown(
        await screen.findByRole("button", {
          name: "Options for category Team",
        }),
        { key: "Enter" },
      );
      fireEvent.click(
        await screen.findByRole("menuitem", { name: en.groups.rename }),
      );
      const input = await screen.findByLabelText(en.groups.nameLabel);
      // Another person renames it between the read and the save.
      const record = store.chat.groups.get(team.id)!;
      record.name = "Squad";
      record.version += 1;
      fireEvent.change(input, { target: { value: "Mine" } });
      fireEvent.click(screen.getByRole("button", { name: en.groups.save }));

      await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
      expect(store.chat.groups.get(team.id)?.name).toBe("Squad");
      expect(await screen.findByText("Squad")).not.toBeNull();
    },
    THIRTY,
  );

  it(
    "deletes a category after confirming: its channels stay, now under the ungrouped heading",
    async () => {
      const { store, team } = mount("owner");
      const held = [...store.chat.spaces.values()].filter(
        (space) => space.groupId === team.id && space.deletedAt === null,
      );
      expect(held.length).toBeGreaterThan(0);
      fireEvent.keyDown(
        await screen.findByRole("button", {
          name: "Options for category Team",
        }),
        { key: "Enter" },
      );
      fireEvent.click(
        await screen.findByRole("menuitem", { name: en.groups.delete }),
      );
      fireEvent.click(
        await screen.findByRole("button", { name: en.groups.deleteConfirm }),
      );

      await waitFor(() => expect(store.chat.groups.has(team.id)).toBe(false));
      for (const space of held) {
        expect(store.chat.spaces.get(space.id)?.groupId).toBeNull();
        expect((await screen.findAllByText(space.name)).length).toBeGreaterThan(
          0,
        );
      }
    },
    THIRTY,
  );
});

describe("Channel order in the workspace (UC-CHT-A04)", () => {
  it(
    "moves a channel with the row menu: the server keeps the new place for everyone",
    async () => {
      const { store } = mount("owner");
      const first = [...store.chat.spaces.values()]
        .filter(
          (space) =>
            space.deletedAt === null &&
            space.archivedAt === null &&
            space.groupId === null &&
            store.chat.activeMembershipOf(space.id, store.getCurrentUser().id),
        )
        .sort(
          (a, b) => a.position - b.position || a.name.localeCompare(b.name),
        )[0]!;
      const before = { position: first.position, groupId: first.groupId };
      const row = (await screen.findAllByText(first.name))[0]!.closest("li")!;
      fireEvent.keyDown(
        within(row).getByRole("button", { name: en.groups.channelMenu }),
        { key: "Enter" },
      );
      fireEvent.click(
        await screen.findByRole("menuitem", {
          name: en.groups.channelMoveDown,
        }),
      );
      await waitFor(() => {
        const after = store.chat.spaces.get(first.id)!;
        expect([after.position, after.groupId]).not.toEqual([
          before.position,
          before.groupId,
        ]);
      });
    },
    THIRTY,
  );
});

describe("Collapse of a category in the workspace (UC-CHT-A04)", () => {
  it(
    "stays collapsed after a reload, for this person only",
    async () => {
      const data = new Map<string, string>();
      const collapseStore = {
        getItem: (key: string) => data.get(key) ?? null,
        setItem: (key: string, value: string) => void data.set(key, value),
      };
      const { team, unmount, store } = mount("owner", { collapseStore });
      const heldName = [...store.chat.spaces.values()].find(
        (space) => space.groupId === team.id && space.deletedAt === null,
      )!.name;
      expect((await screen.findAllByText(heldName)).length).toBeGreaterThan(0);

      fireEvent.click(
        await screen.findByRole("button", { name: "Collapse Team" }),
      );
      await waitFor(() => expect(screen.queryByText(heldName)).toBeNull());
      expect(data.get(collapseKey(WORKSPACE, team.id))).toBe("1");
      unmount();

      mount("owner", { collapseStore });
      await screen.findByRole("button", { name: "Expand Team" });
      expect(screen.queryByText(heldName)).toBeNull();
    },
    THIRTY,
  );
});
