import type { ReactNode } from "react";
import {
  fireEvent,
  render,
  renderHook,
  screen,
  waitFor,
} from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { ChatThreadPanel } from "../web/components/chat-thread-panel";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";
import { en } from "../web/components/chat.copy";
import type { ChatMessage } from "../core/types/chat";

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
  return { store, hooks, queryClient, wrapper };
}

/** The first plain message of the general channel: a thread can start from it. */
async function rootOfGeneral(
  hooks: ReturnType<typeof setup>["hooks"],
): Promise<ChatMessage> {
  const channels = await hooks.service.listChannels(WORKSPACE);
  const general = channels.find((channel) => channel.name === "general")!;
  const page = await hooks.service.listMessages(WORKSPACE, general.id, {}, 50);
  const root = flattenPages({ pageParams: [{}], pages: [page] }).find(
    (message) =>
      message.kind === "user" &&
      !message.deleted &&
      message.threadRootId == null,
  );
  if (!root) throw new Error("no root in the mock");
  return root;
}

describe("ChatThreadPanel", () => {
  it("shows the root and its replies, and sends a reply that can also go to the channel", async () => {
    const { hooks, wrapper } = setup();
    const root = await rootOfGeneral(hooks);
    const onClose = vi.fn();
    const pinnedIds = new Set<string>();
    const view = render(
      <ChatThreadPanel
        hooks={hooks}
        workspaceId={WORKSPACE}
        rootId={root.id}
        viewerId="someone"
        authorName={() => "Author"}
        copy={en}
        locale="en"
        timeZone="UTC"
        spaceLabel="# general"
        pinnedIds={pinnedIds}
        onClose={onClose}
        onReact={vi.fn()}
        onTogglePin={vi.fn()}
      />,
      { wrapper },
    );
    await waitFor(() =>
      expect(screen.getByTestId("chat-thread-replies")).toBeTruthy(),
    );
    expect(screen.getByRole("region", { name: "Thread" })).toBeTruthy();

    const box = screen.getByRole("textbox", { name: "Reply in thread" });
    fireEvent.change(box, { target: { value: "Đã kiểm tra xong" } });
    fireEvent.click(
      screen.getByRole("checkbox", { name: "Also send to channel" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Send" }));
    await waitFor(() =>
      expect(screen.getByText("Đã kiểm tra xong")).toBeTruthy(),
    );
    // The choice stays on for the next reply.
    expect(
      (
        screen.getByRole("checkbox", {
          name: "Also send to channel",
        }) as HTMLInputElement
      ).checked,
    ).toBe(true);

    fireEvent.click(screen.getByRole("button", { name: "Follow" }));
    await waitFor(() =>
      expect(
        screen
          .getByRole("button", { name: "Following" })
          .getAttribute("aria-pressed"),
      ).toBe("true"),
    );
    fireEvent.click(screen.getByRole("button", { name: "Close thread" }));
    expect(onClose).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("keeps the caller's text when the reply cannot be sent", async () => {
    const { hooks, wrapper } = setup();
    const root = await rootOfGeneral(hooks);
    const failing = {
      ...hooks,
      useReplyInThread: () => ({
        mutate: (_input: unknown, options?: { onError?: () => void }) =>
          options?.onError?.(),
        isPending: false,
      }),
    } as unknown as typeof hooks;
    render(
      <ChatThreadPanel
        hooks={failing}
        workspaceId={WORKSPACE}
        rootId={root.id}
        viewerId="someone"
        authorName={() => "Author"}
        copy={en}
        locale="en"
        timeZone="UTC"
        spaceLabel="# general"
        pinnedIds={new Set()}
        onClose={vi.fn()}
        onReact={vi.fn()}
        onTogglePin={vi.fn()}
      />,
      { wrapper },
    );
    await waitFor(() =>
      expect(screen.getByTestId("chat-thread-replies")).toBeTruthy(),
    );
    const box = screen.getByRole("textbox", { name: "Reply in thread" });
    fireEvent.change(box, { target: { value: "keep me" } });
    fireEvent.click(screen.getByRole("button", { name: "Send" }));
    expect((await screen.findByRole("alert")).textContent).toContain(
      "The reply did not send. Your text is kept.",
    );
    expect((box as HTMLTextAreaElement).value).toBe("keep me");
  });
});

describe("threads I follow", () => {
  it("lists the followed threads through the hook (the empty state when none)", async () => {
    const { hooks, wrapper } = setup();
    const { result } = renderHook(
      () => hooks.useFollowedThreads(WORKSPACE, true),
      { wrapper },
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(Array.isArray(result.current.data?.pages[0]?.items)).toBe(true);
  });
});
