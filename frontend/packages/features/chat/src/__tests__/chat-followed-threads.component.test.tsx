import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { FollowedThreadsPanel } from "../web/components/chat-followed-threads";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";
import { en, vi as viCopy } from "../web/components/chat.copy";

const WORKSPACE = mockIds.workspaces.primary;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { hooks, wrapper };
}

function panel(hooks: ReturnType<typeof setup>["hooks"], onOpen = vi.fn()) {
  return (
    <FollowedThreadsPanel
      hooks={hooks}
      workspaceId={WORKSPACE}
      copy={en}
      locale="en"
      timeZone="UTC"
      authorName={() => "Author"}
      channelName={() => "# general"}
      onOpenThread={onOpen}
      onClose={vi.fn()}
    />
  );
}

describe("FollowedThreadsPanel", () => {
  it("says so when no thread is followed, in both languages of the copy", async () => {
    const { hooks, wrapper } = setup();
    render(panel(hooks), { wrapper });
    await waitFor(() =>
      expect(screen.getByText(en.thread.followedEmptyTitle)).toBeTruthy(),
    );
    expect(viCopy.thread.followedEmptyTitle.length).toBeGreaterThan(0);
  });

  it("lists a followed thread, opens it, and shows no unread badge when everything is read", async () => {
    const { hooks, wrapper } = setup();
    const channels = await hooks.service.listChannels(WORKSPACE);
    const general = channels.find((c) => c.name === "general")!;
    const page = await hooks.service.listMessages(
      WORKSPACE,
      general.id,
      {},
      50,
    );
    const root = flattenPages({ pageParams: [{}], pages: [page] }).find(
      (m) => m.kind === "user" && !m.deleted && m.threadRootId == null,
    )!;
    await hooks.service.replyInThread(WORKSPACE, root.id, {
      content: "Phản hồi đầu",
      clientMessageId: crypto.randomUUID(),
      alsoSendToChannel: false,
    });
    await hooks.service.setThreadFollow(WORKSPACE, root.id, {
      following: true,
      muted: false,
    });
    const onOpen = vi.fn();
    render(panel(hooks, onOpen), { wrapper });
    const row = await screen.findByTestId("chat-followed-item");
    expect(row.textContent).toContain("1 reply");
    fireEvent.click(screen.getByRole("button", { name: /Open thread/ }));
    expect(onOpen).toHaveBeenCalledWith(root.id);
    // A new follow starts read, so no unread badge shows.
    expect(screen.queryByTestId("chat-followed-unread")).toBeNull();
    expect(row.getAttribute("data-unread")).toBe("false");
  });
});
