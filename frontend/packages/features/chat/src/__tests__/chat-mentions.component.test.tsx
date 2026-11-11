import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import {
  ChatComposer,
  type ChatComposerMentions,
} from "../web/components/chat-composer";
import { ChatMessageRow } from "../web/components/chat-message-row";
import { ChatMentionProvider } from "../web/components/chat-mention-context";
import { MentionsPanel } from "../web/components/chat-mentions-panel";
import { ChatSidebar } from "../web/components/chat-sidebar";
import { en } from "../web/components/chat.copy";
import type {
  ChatChannel,
  ChatMentionables,
  ChatMessage,
  ChatMention,
  ChatUnreadRow,
} from "../core/types/chat";

const ME = "u-me";
const MINH = "u-minh";
const LAN = "u-lan";

const people = new Map([
  [ME, "Me"],
  [MINH, "Minh Quân"],
  [LAN, "Lan"],
]);

function mentionables(
  overrides: Partial<ChatMentionables> = {},
): ChatMentionables {
  return {
    people: [
      { userId: ME, inSpace: true },
      { userId: MINH, inSpace: true },
      { userId: LAN, inSpace: false },
    ],
    canBroadcast: true,
    broadcastConfirmAbove: 50,
    memberCount: 8,
    ...overrides,
  };
}

function Harness({
  onSend,
  picker = mentionables(),
  extra = {},
}: {
  onSend: (...args: unknown[]) => void;
  picker?: ChatMentionables | undefined;
  extra?: Partial<ChatComposerMentions>;
}) {
  const [value, setValue] = useState("");
  return (
    <ChatComposer
      channelName="general"
      value={value}
      disabledReason={null}
      copy={en}
      onChange={setValue}
      onSend={onSend}
      mentions={{
        selfId: ME,
        mentionables: picker,
        status: picker ? "ready" : "loading",
        people,
        channels: [
          { id: "s-general", name: "general" },
          { id: "s-design", name: "design" },
        ],
        draftKey: "s-1",
        ...extra,
      }}
    />
  );
}

function box() {
  return screen.getByRole("combobox", { name: "Message #general" });
}

function type(text: string) {
  fireEvent.change(box(), { target: { value: text } });
}

describe("composer autocomplete", () => {
  it("opens a listbox on @ with the people, @channel and @here, and the combobox points at the active option", () => {
    render(<Harness onSend={vi.fn()} />);
    type("@");
    const list = screen.getByRole("listbox", { name: "Mention suggestions" });
    const options = within(list).getAllByRole("option");
    expect(options.map((o) => o.textContent)).toEqual([
      expect.stringContaining("@channel"),
      expect.stringContaining("@here"),
      "Minh Quân",
      expect.stringContaining("Lan"),
    ]);
    expect(box().getAttribute("aria-expanded")).toBe("true");
    expect(box().getAttribute("aria-controls")).toBe(list.id);
    expect(box().getAttribute("aria-activedescendant")).toBe(options[0]!.id);
    expect(options[0]!.getAttribute("aria-selected")).toBe("true");
    // The caller is not offered; a person outside the channel says so.
    expect(screen.queryByText("Me")).toBeNull();
    expect(within(list).getByText("Not in this channel")).not.toBeNull();
  });

  it("filters by the typed text without accents", () => {
    render(<Harness onSend={vi.fn()} />);
    type("@quan");
    const options = screen.getAllByRole("option");
    expect(options.map((o) => o.textContent)).toEqual(["Minh Quân"]);
  });

  it("moves with the arrow keys, wraps, and inserts with Enter instead of sending", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    type("@");
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    expect(box().getAttribute("aria-activedescendant")).toBe(
      screen.getAllByRole("option")[1]!.id,
    );
    fireEvent.keyDown(box(), { key: "ArrowUp" });
    fireEvent.keyDown(box(), { key: "ArrowUp" });
    expect(
      screen.getAllByRole("option")[3]!.getAttribute("aria-selected"),
    ).toBe("true");
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    fireEvent.keyDown(box(), { key: "ArrowDown" });
    fireEvent.keyDown(box(), { key: "Enter" });
    expect((box() as HTMLTextAreaElement).value).toBe("@Minh Quân ");
    expect(onSend).not.toHaveBeenCalled();
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("inserts with Tab and closes with Escape, keeping the typed text", () => {
    render(<Harness onSend={vi.fn()} />);
    type("@lan");
    fireEvent.keyDown(box(), { key: "Escape" });
    expect(screen.queryByRole("listbox")).toBeNull();
    expect((box() as HTMLTextAreaElement).value).toBe("@lan");
    type("@min");
    fireEvent.keyDown(box(), { key: "Tab" });
    expect((box() as HTMLTextAreaElement).value).toBe("@Minh Quân ");
  });

  it("inserts a clicked option and sends the structured span the API expects", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    type("hi @min");
    fireEvent.click(screen.getByRole("option", { name: "Minh Quân" }));
    type("hi @Minh Quân please look");
    fireEvent.keyDown(box(), { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("hi @Minh Quân please look", {
      mentions: [{ kind: "user", start: 3, length: 10, userId: MINH }],
    });
  });

  it("lists channels on # and sends a channel link span", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    type("see #des");
    expect(screen.getAllByRole("option").map((o) => o.textContent)).toEqual([
      "design",
    ]);
    fireEvent.keyDown(box(), { key: "Enter" });
    expect((box() as HTMLTextAreaElement).value).toBe("see #design ");
    fireEvent.keyDown(box(), { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("see #design", {
      mentions: [
        { kind: "channel-link", start: 4, length: 7, spaceId: "s-design" },
      ],
    });
  });

  it("sends plain text with no payload when nothing is mentioned", () => {
    const onSend = vi.fn();
    render(<Harness onSend={onSend} />);
    type("just text");
    fireEvent.keyDown(box(), { key: "Enter" });
    expect(onSend).toHaveBeenCalledWith("just text");
  });

  it("offers no @channel or @here without the broadcast action", () => {
    render(
      <Harness
        onSend={vi.fn()}
        picker={mentionables({ canBroadcast: false })}
      />,
    );
    type("@");
    expect(screen.queryByRole("option", { name: /@here/ })).toBeNull();
    type("@here");
    expect(screen.getByTestId("chat-mention-warnings").textContent).toContain(
      "You cannot notify everyone here",
    );
  });
});

describe("mention warning", () => {
  it("says a person outside the channel will not be notified and offers to add them when permitted", () => {
    const onAddMember = vi.fn();
    render(<Harness onSend={vi.fn()} extra={{ onAddMember }} />);
    type("@la");
    fireEvent.keyDown(box(), { key: "Enter" });
    const warnings = screen.getByTestId("chat-mention-warnings");
    expect(warnings.textContent).toContain(
      "Lan is not in this channel and will not be notified.",
    );
    fireEvent.click(within(warnings).getByRole("button", { name: "Add Lan" }));
    expect(onAddMember).toHaveBeenCalledWith(LAN);
  });

  it("is information only without the permission to add", () => {
    render(<Harness onSend={vi.fn()} />);
    type("@la");
    fireEvent.keyDown(box(), { key: "Enter" });
    const warnings = screen.getByTestId("chat-mention-warnings");
    expect(warnings.textContent).toContain("will not be notified");
    expect(within(warnings).queryByRole("button")).toBeNull();
  });
});

describe("broadcast confirmation", () => {
  it("asks before @channel in a large channel and sends with the confirmation", () => {
    const onSend = vi.fn();
    render(
      <Harness onSend={onSend} picker={mentionables({ memberCount: 120 })} />,
    );
    type("@channel standup now");
    fireEvent.keyDown(box(), { key: "Enter" });
    expect(onSend).not.toHaveBeenCalled();
    const dialog = screen.getByRole("alertdialog");
    expect(dialog.textContent).toContain("Notify 120 people?");
    // The safe choice has the focus.
    expect(document.activeElement).toBe(
      within(dialog).getByRole("button", { name: "Keep editing" }),
    );
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Send and notify" }),
    );
    expect(onSend).toHaveBeenCalledWith("@channel standup now", {
      mentions: [{ kind: "broadcast-channel", start: 0, length: 8 }],
      confirmBroadcast: true,
    });
  });

  it("goes back to the draft on Keep editing without sending", () => {
    const onSend = vi.fn();
    render(
      <Harness onSend={onSend} picker={mentionables({ memberCount: 120 })} />,
    );
    type("@here hello");
    fireEvent.keyDown(box(), { key: "Enter" });
    fireEvent.click(screen.getByRole("button", { name: "Keep editing" }));
    expect(screen.queryByRole("alertdialog")).toBeNull();
    expect(onSend).not.toHaveBeenCalled();
    expect(document.activeElement).toBe(box());
  });

  it("sends at once in a channel at or under the threshold", () => {
    const onSend = vi.fn();
    render(
      <Harness onSend={onSend} picker={mentionables({ memberCount: 50 })} />,
    );
    type("@here hello");
    fireEvent.keyDown(box(), { key: "Enter" });
    expect(screen.queryByRole("alertdialog")).toBeNull();
    expect(onSend).toHaveBeenCalledWith("@here hello", {
      mentions: [{ kind: "broadcast-here", start: 0, length: 5 }],
    });
  });
});

function message(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: "m-1",
    spaceId: "s-1",
    sequence: 1,
    kind: "user",
    authorId: "u-other",
    content: "hi @Minh Quân see #design, @here",
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
    mentions: [
      { kind: "user", start: 3, length: 10, userId: MINH, spaceId: null },
      {
        kind: "channel-link",
        start: 18,
        length: 7,
        userId: null,
        spaceId: "s-design",
      },
      {
        kind: "broadcast-here",
        start: 27,
        length: 5,
        userId: null,
        spaceId: null,
      },
    ] satisfies ChatMention[],
    ...overrides,
  };
}

function renderRow(
  msg: ChatMessage,
  viewerId = ME,
  onOpenChannel: (id: string) => void = vi.fn(),
) {
  return render(
    <ChatMentionProvider
      value={{
        viewerId,
        personName: (id) => people.get(id) ?? null,
        channelName: (id) => (id === "s-design" ? "design-team" : null),
        onOpenChannel,
      }}
    >
      <ChatMessageRow
        message={msg}
        author={{ name: "Other" }}
        continued={false}
        isOwn={msg.authorId === viewerId}
        locale="en"
        timeZone="UTC"
        copy={en}
      />
    </ChatMentionProvider>,
  );
}

describe("mention chips", () => {
  it("renders person, channel and broadcast mentions as chips from identities", () => {
    const { container } = renderRow(message());
    const chips = Array.from(container.querySelectorAll("[data-mention]"));
    expect(
      chips.map((c) => [c.getAttribute("data-mention"), c.textContent]),
    ).toEqual([
      ["user", "@Minh Quân"],
      // The channel shows its current name, not the stored text.
      ["channel-link", "#design-team"],
      ["broadcast", "@here"],
    ]);
  });

  it("opens the channel from a #channel chip", () => {
    const onOpen = vi.fn();
    renderRow(message(), ME, onOpen);
    fireEvent.click(
      screen.getByRole("button", { name: "Link to channel design-team" }),
    );
    expect(onOpen).toHaveBeenCalledWith("s-design");
  });

  it("falls back to the stored text for a person who is not in the list", () => {
    const msg = message({
      content: "hi @Ghost",
      mentions: [
        { kind: "user", start: 3, length: 6, userId: "u-gone", spaceId: null },
      ],
    });
    const { container } = renderRow(msg);
    expect(container.querySelector("[data-mention=user]")?.textContent).toBe(
      "@Ghost",
    );
  });

  it("highlights a message that mentions the viewer, by id or by broadcast, and no other", () => {
    const direct = message({
      mentions: [
        { kind: "user", start: 3, length: 10, userId: ME, spaceId: null },
      ],
    });
    const { container, unmount } = renderRow(direct);
    expect(container.querySelector("[data-mentions-me=true]")).not.toBeNull();
    expect(container.textContent).toContain("Mentions you");
    unmount();
    const broadcast = renderRow(message());
    expect(
      broadcast.container.querySelector("[data-mentions-me=true]"),
    ).not.toBeNull();
    broadcast.unmount();
    const other = renderRow(
      message({
        mentions: [
          { kind: "user", start: 3, length: 10, userId: MINH, spaceId: null },
        ],
      }),
    );
    expect(other.container.querySelector("[data-mentions-me=true]")).toBeNull();
  });

  it("tells the sender who was not notified, on their own message only", () => {
    const own = message({
      authorId: ME,
      mentionNotices: [
        { code: "cannot-view", start: 3, length: 10, userId: MINH },
        { code: "no-audience", start: 27, length: 5, userId: null },
      ],
    });
    const { unmount } = renderRow(own);
    const notices = screen.getByTestId("chat-sent-notices");
    expect(notices.textContent).toContain(
      "Minh Quân was not notified because they are not in this channel.",
    );
    expect(notices.textContent).toContain("@here notified nobody");
    unmount();
    renderRow({ ...own, authorId: "u-other" });
    expect(screen.queryByTestId("chat-sent-notices")).toBeNull();
  });
});

function channel(id: string, name: string): ChatChannel {
  return {
    id,
    name,
    topic: null,
    kind: "channel_public",
    isMember: true,
    isArchived: false,
    notificationLevel: "all",
  } as ChatChannel;
}

describe("sidebar mention badges", () => {
  function sidebar(
    unreadBySpace: Map<
      string,
      { count: number; muted: boolean; mentions?: number }
    >,
  ) {
    return render(
      <ChatSidebar
        channels={[channel("c-a", "alpha"), channel("c-b", "beta")]}
        status="ready"
        activeChannelId={null}
        unreadBySpace={unreadBySpace}
        browse={null}
        canCreate={false}
        copy={en}
        onSelect={vi.fn()}
        onCreate={vi.fn()}
        onJoin={vi.fn()}
        onRetry={vi.fn()}
        joiningId={null}
      />,
    );
  }

  it("shows the mention count apart from the unread count and names both for a screen reader", () => {
    sidebar(new Map([["c-a", { count: 5, muted: false, mentions: 2 }]]));
    const button = screen.getByRole("button", { name: /alpha/ });
    expect(within(button).getByTestId("chat-mention-badge").textContent).toBe(
      "@2",
    );
    expect(button.textContent).toContain("5");
    const label = button.getAttribute("aria-label") ?? "";
    expect(label).toContain("5 unread messages");
    expect(label).toContain("2 unread mentions");
  });

  it("shows no badge without a mention, and none in a muted channel", () => {
    sidebar(
      new Map([
        ["c-a", { count: 5, muted: false, mentions: 0 }],
        ["c-b", { count: 5, muted: true, mentions: 3 }],
      ]),
    );
    expect(screen.queryAllByTestId("chat-mention-badge")).toHaveLength(0);
  });
});

describe("mentions panel", () => {
  function row(
    spaceId: string,
    mentionCount: number,
    muted = false,
  ): ChatUnreadRow {
    return {
      spaceId,
      name: spaceId,
      readSequence: 0,
      lastSequence: 3,
      unreadCount: 3,
      firstUnreadSequence: 1,
      mentionCount,
      notificationLevel: "all",
      muted,
    };
  }

  it("lists the spaces with unread mentions, most first, and opens one", () => {
    const onOpen = vi.fn();
    render(
      <MentionsPanel
        rows={[row("a", 1), row("b", 4), row("c", 0), row("d", 9, true)]}
        copy={en}
        nameOf={(r) => `space ${r.spaceId}`}
        onOpen={onOpen}
      />,
    );
    const items = screen.getAllByRole("button");
    expect(items.map((i) => i.textContent)).toEqual([
      "space b4 unread mentions",
      "space a1 unread mention",
    ]);
    fireEvent.click(items[0]!);
    expect(onOpen).toHaveBeenCalledWith("b");
  });

  it("says when nothing is unread", () => {
    render(
      <MentionsPanel
        rows={[row("a", 0)]}
        copy={en}
        nameOf={() => ""}
        onOpen={vi.fn()}
      />,
    );
    expect(
      screen.getByText("No unread mentions. You are all caught up."),
    ).not.toBeNull();
  });
});
