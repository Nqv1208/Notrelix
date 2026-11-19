import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
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
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { STICKERS, stickerArt } from "../core/model/sticker-model";
import { parseChatMessageFrame } from "../core/realtime/chat-frames";
import { chatFailureOf } from "../core/api/chat-failure";
import {
  ChatMessageRow,
  ChatPendingRow,
} from "../web/components/chat-message-row";
import { ChatSticker } from "../web/components/chat-sticker";
import { StickerPickerButton } from "../web/components/chat-sticker-picker";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { en, vi as viCopy } from "../web/components/chat.copy";
import { createChatHooks } from "../web/query/create-chat-hooks";
import type { ChatMessage } from "../core/types/chat";
import type { PendingMessage } from "../core/model/chat-model";

const WORKSPACE = mockIds.workspaces.primary;

function stickerMessage(
  stickerId: string,
  content = "Sticker: Rocket",
): ChatMessage {
  return {
    id: "m-1",
    spaceId: "space-1",
    sequence: 1,
    kind: "user",
    authorId: "user-other",
    content,
    stickerId,
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

describe("sticker model (UC-CHT-I06)", () => {
  it("has unique ids and art for each, and no art for an unknown id", () => {
    expect(new Set(STICKERS.map((s) => s.id)).size).toBe(STICKERS.length);
    expect(stickerArt("rocket")?.glyph).toBe("🚀");
    expect(stickerArt("nope")).toBeUndefined();
    expect(stickerArt(null)).toBeUndefined();
  });

  it("has an English and a Vietnamese label for every sticker", () => {
    for (const { id } of STICKERS) {
      expect(en.stickers.labels[id], id).toBeTruthy();
      expect(viCopy.stickers.labels[id], id).toBeTruthy();
    }
  });

  it("reads the sticker id of a message frame, and nothing for a frame without one", () => {
    const frame = {
      id: "m1",
      sequence: 3,
      authorId: "u1",
      kind: "user",
      content: "Sticker: Heart",
      contentOmitted: false,
      deleted: false,
      createdAt: "2026-11-10T09:00:00.000Z",
      editedAt: null,
      replyToId: null,
      version: 1,
    };

    expect(
      parseChatMessageFrame({ ...frame, stickerId: "heart" })?.stickerId,
    ).toBe("heart");
    expect(parseChatMessageFrame(frame)?.stickerId).toBeNull();
  });
});

describe("ChatSticker", () => {
  it("draws the art with the label as its name", () => {
    render(<ChatSticker stickerId="rocket" copy={en} />);

    expect(screen.getByRole("img", { name: "Rocket" })).not.toBeNull();
  });

  it("names the sticker in Vietnamese in the Vietnamese catalog", () => {
    render(<ChatSticker stickerId="rocket" copy={viCopy} />);

    expect(screen.getByRole("img", { name: "Tên lửa" })).not.toBeNull();
  });

  it("falls back to the preview text for an id this client has no art for", () => {
    render(
      <ChatSticker
        stickerId="future-one"
        copy={en}
        previewText="Sticker: Future"
      />,
    );

    expect(screen.queryByRole("img")).toBeNull();
    expect(screen.getByText("Sticker: Future")).not.toBeNull();
  });
});

describe("sticker in the list", () => {
  it("shows a sticker message as the sticker, not as text, and offers no Edit or Copy text", async () => {
    render(
      <ChatMessageRow
        message={{ ...stickerMessage("rocket"), authorId: "me" }}
        author={{ name: "Me" }}
        continued={false}
        isOwn
        locale="en"
        timeZone="UTC"
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
      />,
    );

    expect(screen.getByRole("img", { name: "Rocket" })).not.toBeNull();
    expect(screen.queryByText("Sticker: Rocket")).toBeNull();
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    const items = (await screen.findAllByRole("menuitem")).map(
      (item) => item.textContent,
    );
    expect(items).toEqual(["Delete message"]);
  });

  it("shows a pending sticker as the sticker, and a failed one keeps Retry", () => {
    const pending: PendingMessage = {
      clientMessageId: "c1",
      spaceId: "space-1",
      content: "",
      stickerId: "party",
      authorId: "me",
      createdAt: "2026-11-10T09:00:00.000Z",
      status: "failed",
      failure: "retryable",
    };
    const onRetry = vi.fn();
    render(
      <ChatPendingRow
        pending={pending}
        author={{ name: "Me" }}
        locale="en"
        timeZone="UTC"
        copy={en}
        onRetry={onRetry}
        onDiscard={vi.fn()}
      />,
    );

    expect(screen.getByRole("img", { name: "Party" })).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: en.list.retrySend }));
    expect(onRetry).toHaveBeenCalledWith(pending);
  });
});

describe("StickerPickerButton", () => {
  it("opens the catalog, sends the chosen sticker and closes", async () => {
    const onPick = vi.fn();
    const onClosed = vi.fn();
    render(
      <StickerPickerButton copy={en} onPick={onPick} onClosed={onClosed} />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Send a sticker" }));
    const grid = await screen.findByRole("group", { name: "Stickers" });
    expect(within(grid).getAllByRole("button")).toHaveLength(STICKERS.length);

    fireEvent.click(within(grid).getByRole("button", { name: "Heart" }));
    expect(onPick).toHaveBeenCalledWith("heart");
    await waitFor(() =>
      expect(screen.queryByRole("group", { name: "Stickers" })).toBeNull(),
    );
  });

  it("is disabled where the composer is", () => {
    render(
      <StickerPickerButton
        copy={en}
        disabled
        onPick={vi.fn()}
        onClosed={vi.fn()}
      />,
    );

    expect(
      (
        screen.getByRole("button", {
          name: "Send a sticker",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});

describe("sending a sticker on the mock server", () => {
  function setup() {
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    return { store, deps, hooks, queryClient };
  }

  it("stores a sticker alone, once for one key, and refuses an unknown id, text with it, and an edit", async () => {
    const { hooks } = setup();
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (c) => c.name === "general",
    )!;

    const first = await hooks.service.sendMessage(WORKSPACE, general.id, {
      content: "",
      clientMessageId: "sticker-key-1",
      stickerId: "rocket",
    });
    expect(first.stickerId).toBe("rocket");
    expect(first.content).toBe("Sticker: Rocket");
    const again = await hooks.service.sendMessage(WORKSPACE, general.id, {
      content: "",
      clientMessageId: "sticker-key-1",
      stickerId: "rocket",
    });
    expect(again.id).toBe(first.id);

    const refusal = (
      input: { content: string; stickerId: string },
      key: string,
    ) =>
      hooks.service
        .sendMessage(WORKSPACE, general.id, { ...input, clientMessageId: key })
        .then(
          () => null,
          (error: unknown) => chatFailureOf(error),
        );
    expect(
      (await refusal({ content: "", stickerId: "not-a-sticker" }, "k2"))?.kind,
    ).toBe("invalid");
    expect(
      (await refusal({ content: "typed", stickerId: "rocket" }, "k3"))?.kind,
    ).toBe("invalid");

    const edit = await hooks.service
      .editMessage(WORKSPACE, first.id, {
        content: "typed",
        expectedVersion: first.version,
      })
      .then(
        () => null,
        (error: unknown) => chatFailureOf(error),
      );
    expect(edit).not.toBeNull();
  });

  it("sendSticker puts a pending sticker in at once and the stored one replaces it", async () => {
    const { hooks, queryClient } = setup();
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (c) => c.name === "general",
    )!;
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(
      () => hooks.useSendMessage(WORKSPACE, general.id, "me"),
      { wrapper },
    );
    let key = "";
    act(() => {
      key = result.current.sendSticker("party", null);
    });

    await waitFor(async () => {
      const page = await hooks.service.listMessages(
        WORKSPACE,
        general.id,
        {},
        50,
      );
      expect(
        page.items.some(
          (m) => m.stickerId === "party" && m.clientMessageId === key,
        ),
      ).toBe(true);
    });
  });
});

describe("ChatWorkspace stickers", () => {
  beforeAll(() => {
    Element.prototype.scrollIntoView ??= () => undefined;
  });

  it("sends the picked sticker to the channel and shows it in the list", async () => {
    const store = new MockStore({ ...defaultConfig, persona: "owner" });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: (input, init) => createMockFetch(store)(input, init),
    });
    const deps = { api: client.api, endpoints: client.endpoints };
    const hooks = createChatHooks(deps);
    const general = (await hooks.service.listChannels(WORKSPACE)).find(
      (c) => c.name === "general",
    )!;
    render(
      <QueryClientProvider
        client={
          new QueryClient({ defaultOptions: { queries: { retry: false } } })
        }
      >
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
    await screen.findByRole("combobox", { name: /Message #general/ });

    fireEvent.click(screen.getByRole("button", { name: "Send a sticker" }));
    fireEvent.click(await screen.findByRole("button", { name: "On fire" }));

    await waitFor(() =>
      expect(screen.getByRole("img", { name: "On fire" })).not.toBeNull(),
    );
    const page = await hooks.service.listMessages(
      WORKSPACE,
      general.id,
      {},
      50,
    );
    expect(page.items.some((m) => m.stickerId === "fire")).toBe(true);
  });
});
