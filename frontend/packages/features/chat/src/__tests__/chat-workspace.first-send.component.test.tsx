import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
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
import { createChatHooks } from "../web/query/create-chat-hooks";
import { chatQueryKeys } from "../query/keys";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

// The owner's first unread message in `general` is 301; 8 more messages put the newest page out of that window.
async function setup() {
  const store = new MockStore({ ...defaultConfig });
  // A test sets `hold` to keep a request back until it opens the gate, or `failNextSend` to refuse one send.
  const net = {
    hold: null as ((url: string, method: string) => boolean) | null,
    gate: null as Promise<void> | null,
    failNextSend: false,
  };
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const url = typeof input === "string" ? input : (input as Request).url;
      const method = (init?.method ?? "GET").toUpperCase();
      if (net.failNextSend && method === "POST" && url.endsWith("/messages")) {
        net.failNextSend = false;
        throw new TypeError("network down");
      }
      if (net.hold?.(url, method) && net.gate) await net.gate;
      return createMockFetch(store)(input, init);
    },
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const hooks = createChatHooks(deps);
  const channels = await hooks.service.listChannels(WORKSPACE);
  const general = channels.find((entry) => entry.name === "general")!;
  for (let index = 1; index <= 8; index += 1) {
    await hooks.service.sendMessage(WORKSPACE, general.id, {
      content: `newer ${index}`,
      clientMessageId: `cid-newer-${index}`,
    });
  }
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
  return {
    deps,
    hooks,
    general,
    queryClient,
    wrapper,
    store,
    net,
    unmount: (): void => undefined,
  };
}

async function openGeneral(s: Awaited<ReturnType<typeof setup>>) {
  const view = render(
    <ChatWorkspace
      deps={s.deps}
      workspaceId={WORKSPACE}
      viewerId={s.store.getCurrentUser().id}
      viewerRole="owner"
      channelId={s.general.id}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={new Map()}
    />,
    { wrapper: s.wrapper },
  );
  const box = await screen.findByRole("combobox", {
    name: /Message #general/,
  });
  // The window opened around the first unread message: the newest ones are not in it.
  await screen.findByText(/Tin nhắn 301\./);
  expect(screen.queryByText("newer 8")).toBeNull();
  s.unmount = view.unmount;
  return box;
}

function copies(text: string) {
  return screen.queryAllByText(text).length;
}

/** Once the bubble shows, it must never blink out between the pending copy and the stored one. */
function watchBlink(text: string) {
  const seen = { shown: false, blinks: 0 };
  const observer = new MutationObserver(() => {
    if (copies(text) > 0) seen.shown = true;
    else if (seen.shown) seen.blinks += 1;
  });
  observer.observe(document.body, { childList: true, subtree: true });
  return { seen, stop: () => observer.disconnect() };
}

describe("ChatWorkspace first send from an older window", () => {
  it("shows the bubble in the window on screen, then switches to the newest window with one copy", async () => {
    const s = await setup();
    const box = await openGeneral(s);

    fireEvent.change(box, { target: { value: "first from older" } });
    const watch = watchBlink("first from older");
    fireEvent.keyDown(box, { key: "Enter" });

    // At once: the bubble is in the window still on screen; the newest window is not loaded yet.
    expect(copies("first from older")).toBe(1);
    expect(screen.queryByText("newer 8")).toBeNull();

    // After the frame: the newest window, the stored message once, no pending copy.
    await screen.findByText("newer 8");
    await waitFor(() => expect(copies("first from older")).toBe(1));
    expect(screen.queryByTestId("chat-pending-message")).toBeNull();
    watch.stop();
    expect(watch.seen.shown).toBe(true);
    expect(watch.seen.blinks).toBe(0);
  });

  it("goes to the newest window already in the cache without a second copy", async () => {
    const s = await setup();
    const newest = await s.hooks.service.listMessages(
      WORKSPACE,
      s.general.id,
      {},
      50,
    );
    s.queryClient.setQueryData(
      [...chatQueryKeys.messages(WORKSPACE, s.general.id), "latest"],
      { pages: [newest], pageParams: [{}] },
    );
    const box = await openGeneral(s);

    fireEvent.change(box, { target: { value: "first from cache" } });
    const watch = watchBlink("first from cache");
    fireEvent.keyDown(box, { key: "Enter" });

    expect(copies("first from cache")).toBe(1);
    expect(screen.queryByText("newer 8")).toBeNull();
    await screen.findByText("newer 8");
    await waitFor(() => expect(copies("first from cache")).toBe(1));
    expect(screen.queryByTestId("chat-pending-message")).toBeNull();
    watch.stop();
    expect(watch.seen.shown).toBe(true);
    expect(watch.seen.blinks).toBe(0);
  });

  it("keeps the bubble past 10 s while the newest window is late, then shows the stored message once", async () => {
    const s = await setup();
    const box = await openGeneral(s);
    vi.useFakeTimers();
    try {
      // The newest window is not read yet, and its next read waits for the gate.
      s.queryClient.removeQueries({
        queryKey: [
          ...chatQueryKeys.messages(WORKSPACE, s.general.id),
          "latest",
        ],
      });
      let open!: () => void;
      s.net.gate = new Promise<void>((resolve) => (open = resolve));
      s.net.hold = (url, method) =>
        method === "GET" &&
        url.includes("/messages") &&
        !url.includes("around=");

      fireEvent.change(box, { target: { value: "late window" } });
      fireEvent.keyDown(box, { key: "Enter" });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(15_000);
      });
      // Stored and 15 s later still one bubble, in the window on screen.
      expect(copies("late window")).toBe(1);
      expect(screen.queryByText("newer 8")).toBeNull();
      vi.useRealTimers();

      open();
      await screen.findByText("newer 8");
      await waitFor(() => expect(copies("late window")).toBe(1));
      expect(screen.queryByTestId("chat-pending-message")).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it("shows a refused send as not sent, and Resend stores it once", async () => {
    const s = await setup();
    const box = await openGeneral(s);
    s.net.failNextSend = true;

    fireEvent.change(box, { target: { value: "will fail once" } });
    fireEvent.keyDown(box, { key: "Enter" });
    fireEvent.click(
      await screen.findByRole("button", { name: en.list.retrySend }),
    );

    await screen.findByText("newer 8");
    await waitFor(() => expect(copies("will fail once")).toBe(1));
    expect(screen.queryByTestId("chat-pending-message")).toBeNull();
    expect(screen.queryByText(en.list.notSent)).toBeNull();
  });

  it("drops a stored send's bubble when the channel is left before any window holds it", async () => {
    const s = await setup();
    const box = await openGeneral(s);
    s.queryClient.removeQueries({
      queryKey: [...chatQueryKeys.messages(WORKSPACE, s.general.id), "latest"],
    });
    s.net.gate = new Promise<void>(() => undefined);
    s.net.hold = (url, method) =>
      method === "GET" && url.includes("/messages") && !url.includes("around=");

    fireEvent.change(box, { target: { value: "left behind" } });
    fireEvent.keyDown(box, { key: "Enter" });
    const pendingKey = chatQueryKeys.pending(WORKSPACE, s.general.id);
    await waitFor(() =>
      expect(
        (
          s.queryClient.getQueryData<{ storedMessageId?: string }[]>(
            pendingKey,
          ) ?? []
        ).some((entry) => entry.storedMessageId !== undefined),
      ).toBe(true),
    );
    s.unmount();
    expect(s.queryClient.getQueryData(pendingKey)).toEqual([]);
  });
});
