import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { en } from "../web/components/chat.copy";
import { ChatWorkspace } from "../web/components/chat-workspace";

const WORKSPACE = mockIds.workspaces.primary;
const GENERAL = "c1000000-0000-4000-8000-000000000001";
const DELETED = "c1000000-0000-4000-8000-0000000000d1";

function mount(viewerRole: "owner" | "guest", onOpenChannel = vi.fn()) {
  const store = new MockStore({ ...defaultConfig });
  const base = store.chat.spaces.get(GENERAL)!;
  store.chat.spaces.set(DELETED, {
    ...base,
    id: DELETED,
    name: "old-campaign",
    deletedAt: new Date(Date.now() - 3 * 86400000).toISOString(),
    deletedBy: store.getCurrentUser().id,
  });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
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
  render(
    <ChatWorkspace
      deps={{ api: client.api, endpoints: client.endpoints }}
      workspaceId={WORKSPACE}
      viewerId={store.getCurrentUser().id}
      viewerRole={viewerRole}
      channelId={null}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={onOpenChannel}
      people={new Map()}
    />,
    { wrapper },
  );
  return { store, onOpenChannel };
}

describe("Deleted channels in the channel column (UC-CHT-A08)", () => {
  it("opens the list in the main pane, restores a channel and brings it back to the column", async () => {
    const { store } = mount("owner");
    expect(screen.queryByText("old-campaign")).toBeNull();

    fireEvent.click(
      await screen.findByRole("button", { name: en.deleted.entry }),
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Restore #old-campaign" }),
    );

    // The row leaves the list and the channel is in the sidebar again.
    await waitFor(() =>
      expect(store.chat.spaces.get(DELETED)?.deletedAt).toBeNull(),
    );
    await screen.findByText(en.deleted.emptyTitle);
    expect(screen.queryByTestId("chat-deleted-channels")).not.toBeNull();
    await waitFor(() =>
      expect(screen.getAllByText("old-campaign").length).toBeGreaterThan(0),
    );
  });

  it("closes the list when a channel is chosen in the column", async () => {
    const { onOpenChannel } = mount("owner");
    fireEvent.click(
      await screen.findByRole("button", { name: en.deleted.entry }),
    );
    await screen.findByTestId("chat-deleted-channels");

    fireEvent.click(await screen.findByRole("button", { name: /general/ }));
    expect(onOpenChannel).toHaveBeenCalled();
    expect(screen.queryByTestId("chat-deleted-channels")).toBeNull();
  });

  it("does not offer the entry to a Guest", async () => {
    mount("guest");
    await screen.findByRole("navigation", { name: en.sidebar.navLabel });
    expect(screen.queryByRole("button", { name: en.deleted.entry })).toBeNull();
  });
});
