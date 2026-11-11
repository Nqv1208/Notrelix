import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import {
  ReactionChips,
  ReactionPicker,
} from "../web/components/chat-reactions";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { PinnedBar } from "../web/components/chat-pins";
import { ReplyChip } from "../web/components/chat-quote";
import { ChatComposer } from "../web/components/chat-composer";
import type { ChatMessage } from "../core/types/chat";
import type { ChatHooks } from "../web/query/create-chat-hooks";
import { en } from "../web/components/chat.copy";

const OTHER = "user-other";

function message(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: "m-1",
    spaceId: "space-1",
    sequence: 1,
    kind: "user",
    authorId: OTHER,
    content: "Lịch go-live đợt 1",
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

/** The hooks are only read when the who-reacted popover opens, which these tests do not do. */
const noHooks = {} as ChatHooks;

describe("ReactionChips", () => {
  it("shows each emoji with its count, and says which one the caller reacted with", () => {
    const onToggle = vi.fn();
    render(
      <ReactionChips
        messageId="m-1"
        workspaceId="ws"
        reactions={[
          { emoji: "👍", count: 4, reactedByMe: true },
          { emoji: "👀", count: 2, reactedByMe: false },
        ]}
        hooks={noHooks}
        authorName={() => "Someone"}
        copy={en}
        onToggle={onToggle}
      />,
    );
    const mine = screen.getByRole("button", { name: "👍 4, you reacted" });
    expect(mine.getAttribute("aria-pressed")).toBe("true");
    const other = screen.getByRole("button", { name: "👀 2" });
    expect(other.getAttribute("aria-pressed")).toBe("false");
    fireEvent.click(mine);
    expect(onToggle).toHaveBeenLastCalledWith("👍", false);
    fireEvent.click(other);
    expect(onToggle).toHaveBeenLastCalledWith("👀", true);
  });

  it("renders nothing without reactions", () => {
    const { container } = render(
      <ReactionChips
        messageId="m-1"
        workspaceId="ws"
        reactions={[]}
        hooks={noHooks}
        authorName={() => "Someone"}
        copy={en}
        onToggle={vi.fn()}
      />,
    );
    expect(container.innerHTML).toBe("");
  });
});

describe("ReactionPicker", () => {
  it("offers exactly the six palette emoji and reports the pick", () => {
    const onPick = vi.fn();
    render(<ReactionPicker copy={en} reacted={["🚀"]} onPick={onPick} />);
    fireEvent.click(screen.getByRole("button", { name: "React" }));
    const group = screen.getByRole("group", { name: "Reactions" });
    const buttons = within(group).getAllByRole("button");
    expect(buttons.map((button) => button.textContent)).toEqual([
      "👍",
      "❤️",
      "😄",
      "🎉",
      "👀",
      "🚀",
    ]);
    expect(
      within(group)
        .getByRole("button", { name: "Remove your 🚀 reaction" })
        .getAttribute("aria-pressed"),
    ).toBe("true");
    fireEvent.click(
      within(group).getByRole("button", { name: "React with 🎉" }),
    );
    expect(onPick).toHaveBeenCalledWith("🎉");
  });
});

describe("ChatMessageRow toolbar and thread chip", () => {
  it("offers react, reply, thread and pin on a message of someone else, and keeps edit to the author", () => {
    const onReply = vi.fn();
    const onOpenThread = vi.fn();
    const onTogglePin = vi.fn();
    render(
      <ChatMessageRow
        message={message()}
        author={{ name: "Other" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone="UTC"
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onReact={vi.fn()}
        onReply={onReply}
        onOpenThread={onOpenThread}
        onTogglePin={onTogglePin}
        thread={{ replyCount: 6, lastReplyAt: null }}
      />,
    );
    const toolbar = screen.getByRole("toolbar");
    fireEvent.click(within(toolbar).getByRole("button", { name: "Reply" }));
    expect(onReply).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1" }),
    );
    fireEvent.click(
      within(toolbar).getByRole("button", { name: "Pin message" }),
    );
    expect(onTogglePin).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1" }),
      true,
    );
    expect(
      within(toolbar).queryByRole("button", { name: "Edit message" }),
    ).toBeNull();
    fireEvent.click(screen.getByTestId("chat-thread-chip"));
    expect(onOpenThread).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m-1" }),
    );
    expect(screen.getByTestId("chat-thread-chip").textContent).toContain(
      "6 replies",
    );
  });

  it("moves focus between toolbar buttons with the arrow keys", () => {
    render(
      <ChatMessageRow
        message={message()}
        author={{ name: "Other" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone="UTC"
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onReply={vi.fn()}
        onOpenThread={vi.fn()}
      />,
    );
    const toolbar = screen.getByRole("toolbar");
    const reply = within(toolbar).getByRole("button", { name: "Reply" });
    const thread = within(toolbar).getByRole("button", { name: "Open thread" });
    reply.focus();
    fireEvent.keyDown(toolbar, { key: "ArrowRight" });
    expect(document.activeElement).toBe(thread);
    fireEvent.keyDown(toolbar, { key: "ArrowLeft" });
    expect(document.activeElement).toBe(reply);
  });

  it("shows a quote of the message it answers and no toolbar on a deleted message", () => {
    const { unmount } = render(
      <ChatMessageRow
        message={message({ replyToId: "m-0" })}
        author={{ name: "Other" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone="UTC"
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        quote={{
          name: "Minh",
          text: "Chốt lịch",
          deleted: false,
          unavailable: false,
        }}
        onReply={vi.fn()}
      />,
    );
    expect(screen.getByText("Reply to Minh")).toBeTruthy();
    expect(screen.getByText("Chốt lịch")).toBeTruthy();
    unmount();
    render(
      <ChatMessageRow
        message={message({ deleted: true, content: null })}
        author={{ name: "Other" }}
        continued={false}
        isOwn={false}
        locale="en"
        timeZone="UTC"
        copy={en}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onReply={vi.fn()}
      />,
    );
    expect(screen.queryByRole("toolbar")).toBeNull();
  });
});

describe("PinnedBar", () => {
  it("is absent with nothing pinned and opens the Pins panel from the newest pin", () => {
    const onOpen = vi.fn();
    const { rerender } = render(
      <PinnedBar
        copy={en}
        count={0}
        newest={null}
        newestAuthor=""
        onOpen={onOpen}
      />,
    );
    expect(screen.queryByTestId("chat-pinned-bar")).toBeNull();
    rerender(
      <PinnedBar
        copy={en}
        count={3}
        newest={message()}
        newestAuthor="Minh"
        onOpen={onOpen}
      />,
    );
    expect(screen.getByTestId("chat-pinned-bar").textContent).toContain(
      "3 pinned messages",
    );
    fireEvent.click(
      screen.getByRole("button", { name: /Show pinned messages: Minh/ }),
    );
    expect(onOpen).toHaveBeenCalledTimes(1);
  });
});

describe("reply chip in the composer", () => {
  it("names the message being answered and cancels the reply", () => {
    const onCancel = vi.fn();
    const { rerender } = render(
      <ReplyChip copy={en} name="Minh" text="Chốt lịch" onCancel={onCancel} />,
    );
    expect(screen.getByTestId("chat-reply-chip").textContent).toContain(
      "Replying to Minh",
    );
    fireEvent.click(screen.getByRole("button", { name: "Cancel reply" }));
    expect(onCancel).toHaveBeenCalledTimes(1);
    rerender(<ReplyChip copy={en} name="Minh" text="x" onCancel={onCancel} />);
  });

  it("sits above the composer text and sends the text without the chip", () => {
    const onSend = vi.fn();
    render(
      <ChatComposer
        channelName="general"
        value="hello"
        disabledReason={null}
        copy={en}
        onChange={vi.fn()}
        onSend={onSend}
        leading={
          <ReplyChip copy={en} name="Minh" text="Chốt" onCancel={vi.fn()} />
        }
      />,
    );
    expect(screen.getByTestId("chat-reply-chip")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Send" }));
    expect(onSend).toHaveBeenCalledWith("hello");
  });
});
