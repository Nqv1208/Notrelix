import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { VirtuosoMockContext } from "react-virtuoso";
import {
  ChatMessageList,
  type ChatMessageListProps,
} from "../web/components/chat-message-list";
import { buildChatRows, type PendingMessage } from "../core/model/chat-model";
import type { ChatMessage } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

const ME = "user-me";
const TZ = "UTC";

function message(
  sequence: number,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
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
    ...overrides,
  };
}

function props(
  messages: ChatMessage[],
  pending: PendingMessage[] = [],
  rest: Partial<ChatMessageListProps> = {},
): ChatMessageListProps {
  return {
    channelName: "general",
    rows: buildChatRows({ messages, pending, firstUnread: null, timeZone: TZ }),
    status: "ready",
    hasOlder: false,
    isLoadingOlder: false,
    hasNewer: false,
    isLoadingNewer: false,
    scrollRequest: null,
    editing: null,
    staleNotice: null,
    viewerId: ME,
    authorName: (id) => (id === ME ? "Me" : "Other person"),
    locale: "en",
    timeZone: TZ,
    copy: en,
    onRetryLoad: vi.fn(),
    onOlderNeeded: vi.fn(),
    onNewerNeeded: vi.fn(),
    onAtBottomChange: vi.fn(),
    onJumpLatest: vi.fn(),
    onEditDraftChange: vi.fn(),
    onEditSave: vi.fn(),
    onEditCancel: vi.fn(),
    onRetryPending: vi.fn(),
    onDiscardPending: vi.fn(),
    pendingEntries: pending,
    ...rest,
  };
}

function sending(clientMessageId: string): PendingMessage {
  return {
    clientMessageId,
    spaceId: "space-1",
    content: "mine",
    authorId: ME,
    createdAt: "2026-11-10T09:05:00.000Z",
    status: "sending",
    failure: null,
  };
}

describe("ChatMessageList: stable rows", () => {
  it("keeps the same row element when a pending send is confirmed", () => {
    const { rerender } = render(
      <ChatMessageList {...props([message(1)], [sending("c-1")])} />,
    );
    const before = screen.getByText("mine").closest("li");
    expect(before).not.toBeNull();
    rerender(
      <ChatMessageList
        {...props([
          message(1),
          message(2, {
            authorId: ME,
            content: "mine",
            clientMessageId: "c-1",
            createdAt: "2026-11-10T09:05:00.000Z",
          }),
        ])}
      />,
    );
    expect(screen.getByText("mine").closest("li")).toBe(before);
  });
});

describe("ChatMessageList: a long history with layout", () => {
  beforeEach(() => {
    vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(
      function (this: HTMLElement) {
        const height = this.tagName === "LI" ? 50 : 600;
        return {
          x: 0,
          y: 0,
          top: 0,
          left: 0,
          right: 760,
          bottom: height,
          width: 760,
          height,
          toJSON: () => ({}),
        };
      },
    );
    vi.spyOn(HTMLElement.prototype, "offsetHeight", "get").mockReturnValue(600);
    vi.spyOn(HTMLElement.prototype, "offsetWidth", "get").mockReturnValue(760);
    // jsdom has no scrolling methods; the list scrolls to keep its place.
    HTMLElement.prototype.scrollBy = vi.fn();
    HTMLElement.prototype.scrollTo = vi.fn();
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  const wrap = (node: React.ReactNode) => (
    <VirtuosoMockContext.Provider
      value={{ viewportHeight: 600, itemHeight: 50 }}
    >
      {node}
    </VirtuosoMockContext.Provider>
  );
  const many = (from: number, to: number) =>
    Array.from({ length: to - from + 1 }, (_, i) => message(from + i));

  it("slides in a new message's row, not the history", async () => {
    // A view tall enough for the whole history: the follow of a new row is a scripted scroll, which has no layout here.
    const tall = (node: React.ReactNode) => (
      <VirtuosoMockContext.Provider
        value={{ viewportHeight: 4000, itemHeight: 50 }}
      >
        {node}
      </VirtuosoMockContext.Provider>
    );
    const { rerender } = render(
      tall(<ChatMessageList {...props(many(1, 30))} />),
    );
    await waitFor(() =>
      expect(
        screen.getByRole("log").querySelectorAll("li").length,
      ).toBeGreaterThan(0),
    );
    rerender(tall(<ChatMessageList {...props(many(1, 31))} />));
    const log = screen.getByRole("log");
    await waitFor(() =>
      expect(log.querySelector("#message-m-31")).not.toBeNull(),
    );
    expect(log.querySelector("#message-m-31")!.className).toContain(
      "animate-in",
    );
    expect(log.querySelector("#message-m-30")!.className).not.toContain(
      "animate-in",
    );
  });

  it("keeps the rows already drawn when an older page is added above", async () => {
    const { rerender } = render(
      wrap(
        <ChatMessageList {...props(many(21, 60), [], { hasOlder: true })} />,
      ),
    );
    const log = screen.getByRole("log");
    await waitFor(() =>
      expect(log.querySelector("#message-m-60")).not.toBeNull(),
    );
    const before = log.querySelector("#message-m-60");
    rerender(
      wrap(<ChatMessageList {...props(many(1, 60), [], { hasOlder: true })} />),
    );
    expect(screen.getByRole("log").querySelector("#message-m-60")).toBe(before);
  });

  it("shows the older-page placeholder in a strip of fixed height above the rows", async () => {
    const { rerender } = render(
      wrap(<ChatMessageList {...props(many(1, 40), [], { hasOlder: true })} />),
    );
    const log = screen.getByRole("log");
    await waitFor(() => expect(log.querySelector("li")).not.toBeNull());
    const strip = () =>
      log.querySelector<HTMLElement>('[style*="height: 36px"]')!;
    const before = strip().style.height;
    expect(before).toBe("36px");
    rerender(
      wrap(
        <ChatMessageList
          {...props(many(1, 40), [], { hasOlder: true, isLoadingOlder: true })}
        />,
      ),
    );
    expect(strip().style.height).toBe(before);
    expect(screen.getByText(en.list.loadingOlder)).toBeTruthy();
  });
});

describe("ChatMessageList: the jump to the newest", () => {
  function scrolledUp() {
    const log = screen.getByRole("log");
    Object.defineProperty(log, "scrollHeight", {
      configurable: true,
      value: 5000,
    });
    Object.defineProperty(log, "clientHeight", {
      configurable: true,
      value: 500,
    });
    log.scrollTop = 0;
    fireEvent.scroll(log);
    return log;
  }

  it("jumps at once when the reader prefers reduced motion", () => {
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: query.includes("reduce"),
      media: query,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    }));
    render(<ChatMessageList {...props([message(1)])} />);
    const log = scrolledUp();
    fireEvent.click(screen.getByRole("button", { name: en.list.jumpLatest }));
    expect(log.scrollTop).toBe(4500);
    vi.unstubAllGlobals();
  });
});

describe("ChatMessageList: under a floating composer", () => {
  it("leaves room equal to the composer under the last row, with a fade and the pill above it", () => {
    render(<ChatMessageList {...props([message(1)])} bottomInset={96} />);
    const log = screen.getByRole("log");
    // The strip under the rows is the composer's height plus the list's own end padding.
    expect(log.querySelector('[style*="height: 120px"]')).not.toBeNull();
    const fade = screen.getByTestId("chat-list-fade");
    expect(fade.className).toContain("from-background");
    expect(fade.className).toContain("pointer-events-none");
    expect(fade.style.height).toBe("136px");
    expect(screen.getByTestId("chat-jump-latest-layer").style.bottom).toBe(
      "108px",
    );
  });

  it("scales the jump pill up a little on hover, with ease-out, and not under reduced motion", () => {
    render(<ChatMessageList {...props([message(1)])} />);
    const pill = screen
      .getByTestId("chat-jump-latest-layer")
      .querySelector("button")!;
    expect(pill.className).toContain("hover:scale-105");
    expect(pill.className).toContain("duration-150");
    expect(pill.className).toContain("ease-out");
    expect(pill.className).toContain("motion-reduce:hover:scale-100");
  });
});
