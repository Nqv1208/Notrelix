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

describe("ChatMessageList", () => {
  it("is a named log that announces new messages politely", () => {
    render(<ChatMessageList {...props({ messages: [message(1)] })} />);
    const log = screen.getByRole("log", { name: "Messages in general" });
    expect(log.getAttribute("aria-live")).toBe("polite");
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

  it("offers edit and delete only on the caller's own message", () => {
    const messages = [
      message(1, { authorId: ME }),
      message(2, { authorId: OTHER }),
    ];
    render(<ChatMessageList {...props({ messages })} />);
    expect(
      screen.getAllByRole("button", { name: /Message actions for/ }),
    ).toHaveLength(1);
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
    expect(screen.getByText("No messages yet")).not.toBeNull();
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
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(onRetry).toHaveBeenCalledWith(
      expect.objectContaining({ clientMessageId: "c-2" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Discard" }));
    expect(onDiscard).toHaveBeenCalledWith("c-2");
  });
});
