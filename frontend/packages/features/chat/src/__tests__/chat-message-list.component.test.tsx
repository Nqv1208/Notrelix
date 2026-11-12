import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import {
  ChatMessageList,
  type ChatMessageListProps,
} from "../web/components/chat-message-list";
import { buildChatRows, type PendingMessage } from "../core/model/chat-model";
import type { ChatMessage } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

const ME = "user-me";
const OTHER = "user-other";
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
    createdAt: "2026-11-10T09:00:00.000Z",
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
    firstUnread?: number | null;
  } = {},
): ChatMessageListProps {
  const {
    messages = [],
    pending = [],
    firstUnread = null,
    ...rest
  } = overrides;
  const rows = buildChatRows({ messages, pending, firstUnread, timeZone: TZ });
  return {
    channelName: "general",
    rows,
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
    onEdit: vi.fn(),
    onEditDraftChange: vi.fn(),
    onEditSave: vi.fn(),
    onEditCancel: vi.fn(),
    onDelete: vi.fn(),
    onRetryPending: vi.fn(),
    onDiscardPending: vi.fn(),
    pendingEntries: pending,
    ...rest,
  };
}

describe("ChatMessageList: a new message while reading above the bottom", () => {
  it("announces only the new message from someone else, politely, once", () => {
    const first = [message(1)];
    const { rerender } = render(
      <ChatMessageList {...props({ messages: first })} />,
    );
    rerender(
      <ChatMessageList
        {...props({
          messages: [...first, message(2, { content: "hello there" })],
        })}
      />,
    );
    const status = screen
      .getAllByRole("status")
      .find(
        (node) =>
          node.getAttribute("aria-live") === "polite" &&
          node.textContent?.includes("hello"),
      );
    expect(status?.textContent).toBe(
      "New message from Other person: hello there",
    );
    // Older text is not read again.
    expect(
      screen
        .getAllByRole("status")
        .some((node) => node.textContent?.includes("text 1")),
    ).toBe(false);
  });

  it("does not announce the caller's own send, and the jump control is never inside the log", () => {
    const { rerender } = render(
      <ChatMessageList {...props({ messages: [message(1)] })} />,
    );
    rerender(
      <ChatMessageList
        {...props({
          messages: [message(1), message(2, { authorId: ME, content: "mine" })],
        })}
      />,
    );
    expect(screen.queryByText("New message from Me: mine")).toBeNull();
    const jump = screen.queryByRole("button", { name: en.list.jumpLatest });
    // The control is rendered after the log, never inside it (it used to float over the last message).
    const log = screen.getByRole("log");
    expect(jump === null || !log.contains(jump)).toBe(true);
  });
});

describe("ChatMessageList", () => {
  it("is a named log; new messages are announced by a polite status line, not by the whole log", () => {
    render(<ChatMessageList {...props({ messages: [message(1)] })} />);
    const log = screen.getByRole("log", { name: "Messages in general" });
    // The log itself is quiet (no reading of the whole list); new messages are announced by a polite status line.
    expect(log.getAttribute("aria-live")).toBe("off");
    expect(within(log).getByText("text 1")).not.toBeNull();
  });

  it("marks the first unread message with a separator labelled New", () => {
    const messages = [message(1, { authorId: ME }), message(2), message(3)];
    const { container } = render(
      <ChatMessageList {...props({ messages, firstUnread: 2 })} />,
    );
    const divider = screen.getByRole("separator", { name: "New" });
    expect(divider.hasAttribute("data-unread-divider")).toBe(true);
    expect(container.querySelectorAll("[data-unread-divider]")).toHaveLength(1);
  });

  it("shows a deleted message as a tombstone with no content", () => {
    const messages = [
      message(1, {
        deleted: true,
        content: null,
        deletedAt: "2026-11-10T09:05:00.000Z",
        version: 2,
      }),
    ];
    render(<ChatMessageList {...props({ messages })} />);
    expect(screen.getByText("This message was deleted")).not.toBeNull();
    expect(screen.queryByText("text 1")).toBeNull();
  });

  it("labels a system message as system, not as a person", () => {
    const messages = [
      message(1, {
        kind: "system",
        authorId: OTHER,
        content: "joined the channel",
      }),
    ];
    render(<ChatMessageList {...props({ messages })} />);
    expect(screen.getByText("joined the channel")).not.toBeNull();
    expect(screen.getByText(/^System:/)).not.toBeNull();
  });

  it("offers edit and delete in a toolbar only on the caller's own message", () => {
    const messages = [
      message(1, { authorId: ME }),
      message(2, { authorId: OTHER }),
    ];
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    render(<ChatMessageList {...props({ messages, onEdit, onDelete })} />);
    expect(screen.getAllByRole("toolbar")).toHaveLength(1);
    expect(
      screen.getAllByRole("button", { name: "Edit message" }),
    ).toHaveLength(1);
    fireEvent.click(screen.getByRole("button", { name: "Edit message" }));
    expect(onEdit).toHaveBeenCalledWith(expect.objectContaining({ id: "m-1" }));
    fireEvent.click(screen.getByRole("button", { name: "Delete message" }));
    expect(onDelete).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1" }),
    );
  });

  it("offers no toolbar on a deleted message of the caller's", () => {
    const messages = [
      message(1, {
        authorId: ME,
        deleted: true,
        content: null,
        deletedAt: "2026-11-10T09:05:00.000Z",
        version: 2,
      }),
    ];
    render(<ChatMessageList {...props({ messages })} />);
    expect(screen.queryByRole("toolbar")).toBeNull();
  });

  it("renders the empty, loading, error and no-access states", () => {
    const { unmount } = render(
      <ChatMessageList {...props({ status: "loading" })} />,
    );
    expect(
      screen.getByText("Loading messages", { selector: "span" }),
    ).not.toBeNull();
    unmount();

    const onRetry = vi.fn();
    const errorView = render(
      <ChatMessageList {...props({ status: "error", onRetryLoad: onRetry })} />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onRetry).toHaveBeenCalledOnce();
    errorView.unmount();

    const { unmount: unmountNoAccess } = render(
      <ChatMessageList {...props({ status: "no-access" })} />,
    );
    expect(screen.getByText("You cannot see this channel")).not.toBeNull();
    unmountNoAccess();

    render(<ChatMessageList {...props({ status: "ready" })} />);
    expect(screen.getByText("This is the start of #general")).not.toBeNull();
  });

  it("shows the error with its detail and a retry, and a way back on a narrow screen", () => {
    const onRetry = vi.fn();
    const onBack = vi.fn();
    render(
      <ChatMessageList
        {...props({
          status: "error",
          onRetryLoad: onRetry,
          onBackToList: onBack,
        })}
      />,
    );
    expect(
      screen.getByText(
        "Something went wrong while loading the history. Messages you have read are kept.",
      ),
    ).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onRetry).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "Back to channels" }));
    expect(onBack).toHaveBeenCalledOnce();
  });

  it("offers the way back from the no-access state only when the screen is narrow", () => {
    const { unmount } = render(
      <ChatMessageList {...props({ status: "no-access" })} />,
    );
    expect(
      screen.queryByRole("button", { name: "Back to channels" }),
    ).toBeNull();
    unmount();
    render(
      <ChatMessageList
        {...props({ status: "no-access", onBackToList: vi.fn() })}
      />,
    );
    expect(
      screen.getByRole("button", { name: "Back to channels" }),
    ).not.toBeNull();
  });

  it("shows a pending send with its state and offers retry and discard when it failed", () => {
    const pending: PendingMessage[] = [
      {
        clientMessageId: "c-1",
        spaceId: "space-1",
        content: "still sending",
        authorId: ME,
        createdAt: "2026-11-10T10:00:00.000Z",
        status: "sending",
        failure: null,
      },
      {
        clientMessageId: "c-2",
        spaceId: "space-1",
        content: "failed one",
        authorId: ME,
        createdAt: "2026-11-10T10:01:00.000Z",
        status: "failed",
        failure: "retryable",
      },
    ];
    const onRetry = vi.fn();
    const onDiscard = vi.fn();
    render(
      <ChatMessageList
        {...props({
          pending,
          onRetryPending: onRetry,
          onDiscardPending: onDiscard,
        })}
      />,
    );
    expect(screen.getByText("Sending")).not.toBeNull();
    expect(screen.getByText("Not sent")).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Resend" }));
    expect(onRetry).toHaveBeenCalledWith(
      expect.objectContaining({ clientMessageId: "c-2" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Discard" }));
    expect(onDiscard).toHaveBeenCalledWith("c-2");
  });
});

describe("ChatMessageList: the thread chip of a root", () => {
  it("shows N replies from the count the server sent with the message, with no realtime projection", () => {
    render(
      <ChatMessageList
        {...props({
          onOpenThread: vi.fn(),
          messages: [
            message(1, {
              threadReplyCount: 3,
              threadLastReplyAt: "2026-11-10T10:00:00.000Z",
            }),
            message(2),
          ],
        })}
      />,
    );
    const chips = screen.getAllByTestId("chat-thread-chip");
    expect(chips).toHaveLength(1);
    expect(chips[0]!.textContent).toContain("3 replies");
  });

  it("takes the higher of the realtime projection and the stored count", () => {
    render(
      <ChatMessageList
        {...props({
          onOpenThread: vi.fn(),
          messages: [message(1, { threadReplyCount: 2 })],
          threadCounts: {
            "m-1": { replyCount: 5, lastReplyAt: "2026-11-10T11:00:00.000Z" },
          },
        })}
      />,
    );
    expect(screen.getByTestId("chat-thread-chip").textContent).toContain(
      "5 replies",
    );
  });
});
