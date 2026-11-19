import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  render,
  renderHook,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { I18nProvider } from "@notrelix/i18n";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { ChatMessageLinkProvider } from "../web/components/chat-link-context";
import { en } from "../web/components/chat.copy";
import { createChatHooks } from "../web/query/create-chat-hooks";
import type { ChatMessage } from "../core/types/chat";

const toastSuccess = vi.hoisted(() => vi.fn());
vi.mock("sonner", () => ({
  toast: { success: toastSuccess, error: vi.fn() },
}));

const WORKSPACE = mockIds.workspaces.primary;

function message(sequence: number): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: "space-1",
    sequence,
    kind: "user",
    authorId: "user-other",
    content: `text ${sequence}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
  };
}

function row(onMarkUnread?: (message: ChatMessage) => void) {
  return (
    <ChatMessageRow
      message={message(7)}
      author={{ name: "Minh Quân" }}
      continued={false}
      isOwn={false}
      locale="en"
      timeZone="UTC"
      copy={en}
      onMarkUnread={onMarkUnread}
    />
  );
}

afterEach(() => {
  Reflect.deleteProperty(navigator, "clipboard");
  toastSuccess.mockClear();
});

describe("Mark unread from here (UC-CHT-I07): the message menu", () => {
  it("offers the entry only with a handler, and hands the message to it", async () => {
    const bare = render(row());
    // Without a handler (or any other action) the message has no More menu at all.
    expect(screen.queryByRole("button", { name: "More actions" })).toBeNull();
    bare.unmount();

    const onMarkUnread = vi.fn();
    render(row(onMarkUnread));
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Mark unread from here" }),
    );
    expect(onMarkUnread).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-7", sequence: 7 }),
    );
  });

  it("says the link was copied after Copy link", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    render(
      <ChatMessageLinkProvider value={(entry) => `/chat?m=${entry.sequence}`}>
        {row()}
      </ChatMessageLinkProvider>,
    );
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    fireEvent.click(await screen.findByRole("menuitem", { name: "Copy link" }));

    expect(writeText).toHaveBeenCalledWith("/chat?m=7");
    await waitFor(() =>
      expect(toastSuccess).toHaveBeenCalledWith("Copied to clipboard"),
    );
  });
});

describe("useMarkRead.markUnreadFrom", () => {
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
      defaultOptions: { queries: { retry: false } },
    });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
    return { hooks, wrapper };
  }

  it("moves the boundary back to just before the message, and never forward", async () => {
    const { hooks, wrapper } = setup();
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (channel) => channel.name === "general",
    )!;
    const before = (await hooks.service.getUnread(WORKSPACE)).spaces.find(
      (entry) => entry.spaceId === general.id,
    )!;
    await hooks.service.markRead(WORKSPACE, general.id, before.lastSequence);

    const { result } = renderHook(
      () => ({
        unread: hooks.useUnreadSummary(WORKSPACE),
        reader: hooks.useMarkRead(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.unread.isSuccess).toBe(true));
    const rowOf = () =>
      result.current.unread.data!.spaces.find(
        (entry) => entry.spaceId === general.id,
      )!;
    expect(rowOf().unreadCount).toBe(0);

    await act(async () => {
      await result.current.reader.markUnreadFrom(general.id, 2);
    });
    await waitFor(() => expect(rowOf().readSequence).toBe(1));
    await waitFor(() => expect(rowOf().unreadCount).toBeGreaterThan(0));

    // A later message is already unread: nothing moves forward.
    await act(async () => {
      await result.current.reader.markUnreadFrom(general.id, 5);
    });
    expect(rowOf().readSequence).toBe(1);
  });

  it("rejects for a space the caller is not in, so the list can say it failed", async () => {
    const { hooks, wrapper } = setup();
    const { result } = renderHook(() => hooks.useMarkRead(WORKSPACE), {
      wrapper,
    });

    await expect(
      act(async () => {
        await result.current.markUnreadFrom(
          "00000000-0000-4000-8000-000000000000",
          1,
        );
      }),
    ).rejects.toBeDefined();
  });
});

describe("ChatWorkspace mark unread from here (UC-CHT-I07)", () => {
  it("lowers the boundary from the message menu, says so, and keeps the open channel unread", async () => {
    Element.prototype.scrollIntoView ??= () => undefined;
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (channel) => channel.name === "general",
    )!;
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
          <ChatWorkspace
            deps={deps}
            workspaceId={WORKSPACE}
            viewerId={store.getCurrentUser().id}
            viewerRole="owner"
            channelId={general.id}
            onOpenChannel={vi.fn()}
            people={new Map<string, string>()}
          />
        </I18nProvider>
      </QueryClientProvider>,
    );
    await waitFor(() =>
      expect(document.querySelector("[data-message-id]")).not.toBeNull(),
    );
    // The channel is read as it opens: the boundary reaches the newest message.
    await waitFor(async () => {
      const row = (await hooks.service.getUnread(WORKSPACE)).spaces.find(
        (entry) => entry.spaceId === general.id,
      )!;
      expect(row.readSequence).toBe(row.lastSequence);
    });

    const target = document.querySelector("[data-message-id]") as HTMLElement;
    fireEvent.keyDown(
      within(target).getByRole("button", { name: "More actions" }),
      { key: "Enter" },
    );
    fireEvent.click(
      await screen.findByRole("menuitem", { name: "Mark unread from here" }),
    );

    await waitFor(() =>
      expect(toastSuccess).toHaveBeenCalledWith(
        "Marked unread from this message",
        expect.anything(),
      ),
    );
    const after = (await hooks.service.getUnread(WORKSPACE)).spaces.find(
      (entry) => entry.spaceId === general.id,
    )!;
    expect(after.readSequence).toBeLessThan(after.lastSequence);
    expect(after.unreadCount).toBeGreaterThan(0);
  });
});
