import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, within } from "@notrelix/testing";
import {
  ChatMessageList,
  dayLabel,
  type ChatMessageListProps,
} from "../web/components/chat-message-list";
import {
  ChatMessageRow,
  bubbleShape,
  replyCaption,
} from "../web/components/chat-message-row";
import { ChatTypingIndicator } from "../web/components/chat-typing-indicator";
import { ChatConversationHeader } from "../web/components/chat-conversation-view";
import type { ChatConversation } from "../core/types/chat";
import { buildChatRows, type PendingMessage } from "../core/model/chat-model";
import type { ChatMessage } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

const ME = "user-me";
const OTHER = "user-other";
const THIRD = "user-third";
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
    authorId: OTHER,
    content: `text ${sequence}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: `2026-11-10T09:0${sequence % 10}:00.000Z`,
    editedAt: null,
    deletedAt: null,
    version: 1,
    ...overrides,
  };
}

function props(
  overrides: Partial<ChatMessageListProps> & {
    messages?: ChatMessage[];
    pending?: PendingMessage[];
  } = {},
): ChatMessageListProps {
  const { messages = [], pending = [], ...rest } = overrides;
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
    authorName: (id) =>
      id === ME ? "Me" : id === OTHER ? "Minh Quân" : "Thu Hà",
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

function rowOf(id: string): HTMLElement {
  return document.querySelector(`[data-message-id="${id}"]`) as HTMLElement;
}

function barOf(id: string): HTMLElement {
  return within(rowOf(id)).getByRole("toolbar");
}

// jsdom has no PointerEvent: without one, fireEvent drops pointerType and a touch reads as a mouse.
beforeAll(() => {
  if (typeof window.PointerEvent === "undefined") {
    class PointerEventShim extends MouseEvent {
      readonly pointerType: string;
      constructor(type: string, init: PointerEventInit = {}) {
        super(type, init);
        this.pointerType = init.pointerType ?? "mouse";
      }
    }
    Object.defineProperty(window, "PointerEvent", {
      configurable: true,
      value: PointerEventShim,
    });
  }
});

afterEach(() => {
  vi.useRealTimers();
});

describe("Messenger layout: own messages right, others left", () => {
  it("aligns the caller's bubbles to the end with the accent fill, and others' to the start with the neutral fill", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1, { authorId: ME }), message(2)],
        })}
      />,
    );
    const own = rowOf("m-1");
    const other = rowOf("m-2");
    expect(own.getAttribute("data-own")).toBe("true");
    expect(own.className).toContain("flex-row-reverse");
    expect(other.getAttribute("data-own")).toBe("false");
    expect(other.className).not.toContain("flex-row-reverse");
    const ownBubble = own.querySelector("[data-bubble]") as HTMLElement;
    const otherBubble = other.querySelector("[data-bubble]") as HTMLElement;
    expect(ownBubble.getAttribute("data-bubble")).toBe("own");
    expect(ownBubble.className).toContain("bg-primary");
    expect(ownBubble.className).toContain("text-primary-foreground");
    expect(otherBubble.className).toContain("bg-sidebar-accent");
    expect(otherBubble.className).toContain("text-sidebar-accent-foreground");
  });

  it("keeps a bubble to three quarters of the column and wraps long unbroken text", () => {
    const long = "x".repeat(400);
    render(
      <ChatMessageList
        {...props({ messages: [message(1, { content: long })] })}
      />,
    );
    const column = within(rowOf("m-1")).getByTestId("chat-message-column");
    expect(column.className).toContain("max-w-[min(75%,36rem)]");
    expect(column.className).toContain("min-w-0");
    const text = screen.getByText(long);
    expect(text.className).toContain("wrap-anywhere");
    expect(text.className).toContain("break-words");
    expect(text.className).toContain("whitespace-pre-wrap");
  });

  it("names a channel group once at its start, shows the avatar once at its end, and gives the caller neither", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [
            message(1),
            message(2),
            message(3),
            message(4, { authorId: ME }),
          ],
        })}
      />,
    );
    expect(screen.getAllByText("Minh Quân")).toHaveLength(1);
    expect(within(rowOf("m-1")).queryByText("Minh Quân")).not.toBeNull();
    const avatars = (id: string) =>
      rowOf(id).querySelectorAll('[data-avatar-slot] [data-slot="avatar"]')
        .length;
    expect(avatars("m-1")).toBe(0);
    expect(avatars("m-2")).toBe(0);
    expect(avatars("m-3")).toBe(1);
    expect(rowOf("m-4").querySelector("[data-avatar-slot]")).toBeNull();
    expect(within(rowOf("m-4")).queryByText("Me")).toBeNull();
    // The corners toward the avatar are reduced inside the group.
    expect(
      (rowOf("m-2").querySelector("[data-bubble]") as HTMLElement).className,
    ).toContain("rounded-ss-sm");
  });

  it("puts the clock time beside the author name at the start of a group and beside Sent, as machine-readable time elements", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1), message(2), message(3, { authorId: ME })],
        })}
      />,
    );
    const start = rowOf("m-1").querySelector("p time") as HTMLTimeElement;
    expect(start).not.toBeNull();
    expect(start.getAttribute("datetime")).toMatch(/^\d{4}-\d{2}-\d{2}T/);
    expect(start.getAttribute("title")).toBeTruthy();
    expect(start.className).toContain("tabular-nums");
    // Inside the group the name line, and so its time, is not repeated.
    expect(rowOf("m-2").querySelector("p time")).toBeNull();
    const sent = within(rowOf("m-3")).getByText("Sent").parentElement;
    expect(sent?.querySelector("time")?.getAttribute("datetime")).toMatch(
      /^\d{4}-\d{2}-\d{2}T/,
    );
  });

  it("shows no names in a 1-1 conversation", () => {
    render(
      <ChatMessageList
        {...props({ messages: [message(1), message(2)], showNames: false })}
      />,
    );
    expect(screen.queryByText("Minh Quân")).toBeNull();
  });

  it("gives each message an accessible name with who and when", () => {
    render(
      <ChatMessageList
        {...props({ messages: [message(1, { authorId: ME }), message(2)] })}
      />,
    );
    expect(rowOf("m-1").textContent).toContain("You, 9:01 AM: text 1");
    expect(rowOf("m-2").textContent).toContain("Minh Quân, 9:02 AM: text 2");
  });

  it("shows Sent under the caller's newest message only, and edited under an edited one", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [
            message(1, { authorId: ME, edited: true }),
            message(2),
            message(3, { authorId: ME }),
          ],
        })}
      />,
    );
    expect(within(rowOf("m-1")).getByText("edited")).toBeTruthy();
    expect(within(rowOf("m-1")).queryByText("Sent")).toBeNull();
    expect(within(rowOf("m-3")).getByText("Sent")).toBeTruthy();
  });

  it("shows a time separator after a long pause and Today on the day separator", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [
            message(1, { createdAt: "2026-11-10T09:00:00.000Z" }),
            message(2, { createdAt: "2026-11-10T10:00:00.000Z" }),
          ],
        })}
      />,
    );
    const separator = screen.getByTestId("chat-time-separator");
    expect(separator.textContent).toBe("10:00 AM");
    expect(separator.querySelector("time")!.className).toContain(
      "whitespace-nowrap",
    );
  });

  it("labels today and yesterday on the day separator", () => {
    const now = new Date("2026-11-10T12:00:00.000Z");
    expect(dayLabel("2026-11-10", "en", en, TZ, now)).toBe("Today");
    expect(dayLabel("2026-11-09", "en", en, TZ, now)).toBe("Yesterday");
    expect(dayLabel("2026-11-01", "en", en, TZ, now)).toContain("November");
  });
});

describe("Messenger message kinds", () => {
  it("shows one to three emoji large without a bubble", () => {
    render(
      <ChatMessageList
        {...props({ messages: [message(1, { content: "👍🎉" })] })}
      />,
    );
    const bubble = rowOf("m-1").querySelector("[data-bubble]") as HTMLElement;
    expect(bubble.getAttribute("data-bubble")).toBe("emoji");
    expect(bubble.getAttribute("data-emoji-count")).toBe("2");
    expect(bubble.className).toContain("text-4xl");
  });

  it("shows a tombstone as a muted outlined bubble with no actions, and a moderator removal as such", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [
            message(1, { authorId: ME, deleted: true, content: null }),
            message(2, {
              deleted: true,
              content: null,
              removedByModerator: true,
            }),
          ],
          onEdit: vi.fn(),
          onDelete: vi.fn(),
          onReply: vi.fn(),
          onReact: vi.fn(),
        })}
      />,
    );
    const tomb = rowOf("m-1").querySelector("[data-bubble]") as HTMLElement;
    expect(tomb.getAttribute("data-bubble")).toBe("tombstone");
    expect(tomb.className).toContain("italic");
    expect(tomb.className).toContain("border");
    expect(screen.queryByRole("toolbar")).toBeNull();
    expect(
      screen.getByText("This message was removed by a moderator"),
    ).toBeTruthy();
  });

  it("shows a system line as a centred pill without avatar or bubble", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1, { kind: "system", content: "joined" })],
        })}
      />,
    );
    const line = screen.getByTestId("chat-system-message");
    expect(line.className).toContain("justify-center");
    expect(line.querySelector("[data-bubble]")).toBeNull();
    expect(line.querySelector('[data-slot="avatar"]')).toBeNull();
  });

  it("captions a reply: You replied to …, … replied to you", () => {
    const quote = {
      name: "Minh Quân",
      text: "hi",
      deleted: false,
      unavailable: false,
      authorId: OTHER,
    };
    expect(replyCaption(quote, true, "Me", ME, en)).toBe(
      "You replied to Minh Quân",
    );
    expect(
      replyCaption({ ...quote, authorId: ME }, false, "Minh Quân", ME, en),
    ).toBe("Minh Quân replied to you");
    expect(replyCaption(quote, false, "Thu Hà", ME, en)).toBe(
      "Thu Hà replied to Minh Quân",
    );
    expect(replyCaption(null, true, "Me", ME, en)).toBeNull();
  });

  it("rounds a single bubble fully and narrows the inner corners of a run on the author's side", () => {
    expect(bubbleShape(true, false, true)).toBe("rounded-2xl");
    expect(bubbleShape(true, true, false)).toContain("rounded-se-sm");
    expect(bubbleShape(true, true, false)).toContain("rounded-ee-sm");
    expect(bubbleShape(false, false, false)).toContain("rounded-es-sm");
    expect(bubbleShape(false, true, true)).toContain("rounded-ss-sm");
  });
});

describe("Messenger action bar", () => {
  it("shows exactly one action bar at a time: the hovered row's", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1), message(2, { authorId: THIRD })],
          onReply: vi.fn(),
          onReact: vi.fn(),
        })}
      />,
    );
    expect(barOf("m-1").getAttribute("data-visible")).toBe("false");
    fireEvent.pointerEnter(rowOf("m-1"), { pointerType: "mouse" });
    expect(barOf("m-1").getAttribute("data-visible")).toBe("true");
    fireEvent.pointerEnter(rowOf("m-2"), { pointerType: "mouse" });
    expect(barOf("m-1").getAttribute("data-visible")).toBe("false");
    expect(barOf("m-2").getAttribute("data-visible")).toBe("true");
    fireEvent.pointerLeave(rowOf("m-2"));
    expect(barOf("m-2").getAttribute("data-visible")).toBe("false");
  });

  it("opens on keyboard focus, hides the hovered one, and closes on Escape", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1), message(2, { authorId: THIRD })],
          onReply: vi.fn(),
        })}
      />,
    );
    fireEvent.pointerEnter(rowOf("m-1"), { pointerType: "mouse" });
    const reply = within(barOf("m-2")).getByRole("button", { name: "Reply" });
    act(() => reply.focus());
    expect(barOf("m-2").getAttribute("data-visible")).toBe("true");
    expect(barOf("m-1").getAttribute("data-visible")).toBe("false");
    fireEvent.keyDown(reply, { key: "Escape" });
    expect(barOf("m-2").getAttribute("data-visible")).toBe("false");
  });

  it("anchors the bar on the side facing the middle: before own bubbles, after others'", () => {
    render(
      <ChatMessageList
        {...props({
          messages: [message(1, { authorId: ME }), message(2)],
          onReply: vi.fn(),
        })}
      />,
    );
    expect(barOf("m-1").getAttribute("data-side")).toBe("start");
    expect(barOf("m-1").className).toContain("end-full");
    expect(barOf("m-2").getAttribute("data-side")).toBe("end");
    expect(barOf("m-2").className).toContain("start-full");
    expect(barOf("m-2").className).toContain("top-1/2");
  });

  it("lists the More menu by what the viewer may do: the author, a moderator", async () => {
    const own = render(
      <ChatMessageRow
        message={message(1, { authorId: ME })}
        author={{ name: "Me" }}
        continued={false}
        isOwn
        locale="en"
        timeZone={TZ}
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onModerate={vi.fn()}
        onOpenThread={vi.fn()}
        onTogglePin={vi.fn()}
        pinned
      />,
    );
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    expect(
      (await screen.findAllByRole("menuitem")).map((item) => item.textContent),
    ).toEqual([
      "Reply in thread",
      "Unpin message",
      "Edit message",
      "Delete message",
    ]);
    own.unmount();

    render(
      <ChatMessageRow
        message={message(2)}
        author={{ name: "Minh Quân" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone={TZ}
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onModerate={vi.fn()}
      />,
    );
    fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
      key: "Enter",
    });
    expect(
      (await screen.findAllByRole("menuitem")).map((item) => item.textContent),
    ).toEqual(["Delete as moderator"]);
  });

  it("offers Copy text where the clipboard is available", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    try {
      render(
        <ChatMessageRow
          message={message(1)}
          author={{ name: "Minh Quân" }}
          continued={false}
          isOwn={false}
          locale="en"
          timeZone={TZ}
          copy={en}
        />,
      );
      fireEvent.keyDown(screen.getByRole("button", { name: "More actions" }), {
        key: "Enter",
      });
      fireEvent.click(
        await screen.findByRole("menuitem", { name: "Copy text" }),
      );
      expect(writeText).toHaveBeenCalledWith("text 1");
    } finally {
      Reflect.deleteProperty(navigator, "clipboard");
    }
  });

  it("opens the action sheet on a long press by touch", () => {
    vi.useFakeTimers();
    const onReply = vi.fn();
    render(
      <ChatMessageRow
        message={message(1)}
        author={{ name: "Minh Quân" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone={TZ}
        copy={en}
        onReply={onReply}
        onReact={vi.fn()}
      />,
    );
    fireEvent.pointerDown(rowOf("m-1"), {
      pointerType: "touch",
      clientX: 10,
      clientY: 10,
    });
    expect(screen.queryByTestId("chat-message-sheet")).toBeNull();
    act(() => {
      vi.advanceTimersByTime(500);
    });
    const sheet = screen.getByTestId("chat-message-sheet");
    expect(
      within(sheet).getAllByRole("button", { name: /React with/ }),
    ).toHaveLength(6);
    const reply = within(sheet).getByRole("button", { name: "Reply" });
    expect(reply.className).toContain("min-h-11");
    fireEvent.click(reply);
    expect(onReply).toHaveBeenCalled();
  });

  it("does not open the sheet when the touch moves (a scroll), nor show the hover bar on touch", () => {
    vi.useFakeTimers();
    render(
      <ChatMessageRow
        message={message(1)}
        author={{ name: "Minh Quân" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone={TZ}
        copy={en}
        onReply={vi.fn()}
      />,
    );
    fireEvent.pointerDown(rowOf("m-1"), {
      pointerType: "touch",
      clientX: 10,
      clientY: 10,
    });
    fireEvent.pointerMove(rowOf("m-1"), {
      pointerType: "touch",
      clientX: 10,
      clientY: 60,
    });
    act(() => {
      vi.advanceTimersByTime(500);
    });
    expect(screen.queryByTestId("chat-message-sheet")).toBeNull();
    fireEvent.pointerEnter(rowOf("m-1"), { pointerType: "touch" });
    expect(barOf("m-1").getAttribute("data-visible")).toBe("false");
  });
});

describe("Messenger typing bubble", () => {
  it("closes the list with a decorative typing bubble while the live region says who is typing", () => {
    render(
      <>
        <ChatMessageList
          {...props({ messages: [message(1)], typingIds: [OTHER] })}
        />
        <ChatTypingIndicator names={["Minh Quân"]} copy={en} />
      </>,
    );
    const bubble = screen.getByTestId("chat-typing-bubble");
    expect(bubble.closest('[aria-hidden="true"]')).not.toBeNull();
    const status = screen.getByTestId("chat-typing-indicator");
    expect(status.getAttribute("role")).toBe("status");
    expect(status.getAttribute("aria-live")).toBe("polite");
    expect(status.textContent).toBe("Minh Quân is typing…");
  });

  it("shows no typing bubble when nobody types", () => {
    render(<ChatMessageList {...props({ messages: [message(1)] })} />);
    expect(screen.queryByTestId("chat-typing-bubble")).toBeNull();
  });
});

describe("Messenger conversation header", () => {
  const conversation: ChatConversation = {
    id: "dm-1",
    kind: "direct",
    name: null,
    participants: [
      { userId: ME, role: "member", joinedAt: "2026-11-01T00:00:00.000Z" },
      { userId: OTHER, role: "member", joinedAt: "2026-11-01T00:00:00.000Z" },
    ],
    isHidden: false,
    notificationLevel: "all",
    unreadCount: 0,
    lastMessage: null,
    lastActivityAt: "2026-11-10T09:00:00.000Z",
    version: 1,
    createdAt: "2026-11-01T00:00:00.000Z",
  };

  function header(presence?: "online" | "idle" | "offline") {
    return render(
      <ChatConversationHeader
        conversation={conversation}
        viewerId={ME}
        nameOf={(id) => (id === ME ? "Me" : "Minh Quân")}
        copy={en}
        detailsOpen={false}
        blocked={false}
        presenceOf={presence ? () => presence : undefined}
        onOpenList={vi.fn()}
        onToggleDetails={vi.fn()}
        onHide={vi.fn()}
      />,
    );
  }

  it("says Active now for a person who is online, Away when idle, and Direct message otherwise", () => {
    const online = header("online");
    expect(screen.getByTestId("chat-conversation-subtitle").textContent).toBe(
      "Active now",
    );
    online.unmount();
    const idle = header("idle");
    expect(screen.getByTestId("chat-conversation-subtitle").textContent).toBe(
      "Away",
    );
    idle.unmount();
    header();
    expect(screen.getByTestId("chat-conversation-subtitle").textContent).toBe(
      "Direct message",
    );
  });
});
