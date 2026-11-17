import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import {
  ChatSidebar,
  groupChannels,
  unreadBadge,
  type ChatSidebarProps,
} from "../web/components/chat-sidebar";
import type { ChatChannel, ChatConversation } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

function channel(
  id: string,
  name: string,
  overrides: Partial<ChatChannel> = {},
): ChatChannel {
  return {
    id,
    name,
    topic: null,
    kind: "channel_public",
    isMember: true,
    isArchived: false,
    notificationLevel: "all",
    ...overrides,
  } as ChatChannel;
}

function props(overrides: Partial<ChatSidebarProps> = {}): ChatSidebarProps {
  return {
    channels: [
      channel("c-general", "general"),
      channel("c-leads", "leadership", { kind: "channel_private" }),
    ],
    status: "ready",
    activeChannelId: "c-general",
    unreadBySpace: new Map([["c-general", { count: 120, muted: false }]]),
    browse: null,
    canCreate: true,
    copy: en,
    onSelect: vi.fn(),
    onCreate: vi.fn(),
    onJoin: vi.fn(),
    onRetry: vi.fn(),
    joiningId: null,
    ...overrides,
  };
}

describe("unreadBadge", () => {
  it("shows nothing for zero and 99+ above 99", () => {
    expect(unreadBadge(0)).toBeNull();
    expect(unreadBadge(7)).toBe("7");
    expect(unreadBadge(99)).toBe("99");
    expect(unreadBadge(100)).toBe("99+");
  });
});

describe("ChatSidebar", () => {
  it("marks the open channel as the current page and names its unread state for a screen reader", () => {
    render(<ChatSidebar {...props()} />);
    const open = screen.getByRole("button", { name: /general/ });
    expect(open.getAttribute("aria-current")).toBe("page");
    expect(open.getAttribute("aria-label") ?? "").toContain("Unread messages");
    expect(
      screen
        .getByRole("button", { name: /leadership/ })
        .getAttribute("aria-current"),
    ).toBeNull();
  });

  it("selects a channel when it is clicked", () => {
    const onSelect = vi.fn();
    render(<ChatSidebar {...props({ onSelect })} />);
    fireEvent.click(screen.getByRole("button", { name: /leadership/ }));
    expect(onSelect).toHaveBeenCalledWith("c-leads");
  });

  it("offers to create a channel only when the caller can", () => {
    const { unmount } = render(
      <ChatSidebar {...props({ canCreate: false })} />,
    );
    expect(screen.queryByRole("button", { name: "New channel" })).toBeNull();
    unmount();
    render(<ChatSidebar {...props({ canCreate: true })} />);
    expect(screen.getByRole("button", { name: "New channel" })).not.toBeNull();
  });

  it("lists the public channels to join and joins one", () => {
    const onJoin = vi.fn();
    render(
      <ChatSidebar
        {...props({
          browse: {
            status: "ready",
            channels: [channel("c-random", "random")],
          },
          onJoin,
        })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Join #random" }));
    expect(onJoin).toHaveBeenCalledWith("c-random");
  });

  it("offers a retry when the channel list fails", () => {
    const onRetry = vi.fn();
    render(<ChatSidebar {...props({ status: "error", onRetry })} />);
    expect(screen.getByText("Channels could not load.")).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("groups the channels under headings: the ungrouped ones first, then each group numbered", () => {
    render(
      <ChatSidebar
        {...props({
          channels: [
            channel("c-general", "general"),
            channel("c-erp", "erp-rollout", { groupId: "g-2" }),
            channel("c-launch", "launch-2026", { groupId: "g-2" }),
            channel("c-ke-toan", "ke-toan", { groupId: "g-9" }),
          ],
        })}
      />,
    );
    const headings = screen
      .getAllByRole("heading", { level: 3 })
      .map((node) => node.textContent);
    expect(headings).toEqual(["Channels", "Group 1", "Group 2"]);
    expect(screen.getByRole("button", { name: /erp-rollout/ })).not.toBeNull();
  });

  it("shows the real category name, and the numbered fallback for a group whose name is not read", () => {
    render(
      <ChatSidebar
        {...props({
          channels: [
            channel("c-general", "general"),
            channel("c-erp", "erp-rollout", { groupId: "g-2" }),
            channel("c-ke-toan", "ke-toan", { groupId: "g-9" }),
          ],
          groupNames: new Map([
            ["g-2", "  Projects "],
            ["g-9", "   "],
          ]),
        })}
      />,
    );
    const headings = screen
      .getAllByRole("heading", { level: 3 })
      .map((node) => node.textContent);
    expect(headings).toEqual(["Channels", "Projects", "Group 2"]);
  });

  const people = { me: "Thu Hà", thao: "Nguyễn Thảo", quan: "Trần Quân" };
  const nameOf = (id: string) => people[id as keyof typeof people] ?? "?";
  function dm(id: string, other: string): ChatConversation {
    return {
      id,
      kind: "direct",
      name: null,
      participants: [
        { userId: "me", role: "member", joinedAt: "2026-11-01T08:00:00Z" },
        { userId: other, role: "member", joinedAt: "2026-11-01T08:00:00Z" },
      ],
      isHidden: false,
      notificationLevel: "all",
      unreadCount: 0,
      lastMessage: null,
      lastActivityAt: "2026-11-10T09:00:00Z",
      version: 1,
      createdAt: "2026-11-01T08:00:00Z",
    };
  }
  function withDms(
    overrides: Partial<ChatSidebarProps> = {},
    onPick = vi.fn(),
  ) {
    return props({
      conversations: {
        conversations: [dm("d-thao", "thao"), dm("d-quan", "quan")],
        status: "ready",
        activeId: null,
        viewerId: "me",
        locale: "en",
        timeZone: "UTC",
        nameOf,
        unreadOf: () => 0,
        onSelect: onPick,
        onRetry: vi.fn(),
      },
      ...overrides,
    });
  }

  it("filters channels and conversations at once, ignoring accents", () => {
    render(<ChatSidebar {...withDms()} />);
    const field = screen.getByRole("combobox");
    expect(field.getAttribute("aria-keyshortcuts")).toBe("/");
    fireEvent.change(field, { target: { value: "thao" } });
    const options = screen.getAllByRole("option");
    expect(options).toHaveLength(1);
    expect(options[0]!.getAttribute("aria-label")).toContain("Nguyễn Thảo");
    fireEvent.change(field, { target: { value: "lead" } });
    expect(screen.getAllByRole("option")).toHaveLength(1);
  });

  it("moves through matches with the arrows, opens one with Enter and clears with Escape", () => {
    const onPick = vi.fn();
    const onSelect = vi.fn();
    render(<ChatSidebar {...withDms({ onSelect }, onPick)} />);
    const field = screen.getByRole("combobox") as HTMLInputElement;
    fireEvent.change(field, { target: { value: "n" } });
    // "n": channels general and leadership(no n? "leadership" has none) -> general, then Thảo and Quân.
    const first = field.getAttribute("aria-activedescendant");
    fireEvent.keyDown(field, { key: "ArrowDown" });
    expect(field.getAttribute("aria-activedescendant")).not.toBe(first);
    fireEvent.keyDown(field, { key: "ArrowUp" });
    fireEvent.keyDown(field, { key: "Enter" });
    expect(onSelect).toHaveBeenCalledWith("c-general");
    expect(field.value).toBe("");
    fireEvent.change(field, { target: { value: "quan" } });
    fireEvent.keyDown(field, { key: "Enter" });
    expect(onPick).toHaveBeenCalledWith("d-quan");
    fireEvent.change(field, { target: { value: "zzz" } });
    fireEvent.keyDown(field, { key: "Escape" });
    expect(field.value).toBe("");
  });

  it("says nothing matched and offers the message search with the text", () => {
    const onSearch = vi.fn();
    render(<ChatSidebar {...withDms({ onSearch })} />);
    fireEvent.change(screen.getByRole("combobox"), {
      target: { value: "zzz" },
    });
    expect(screen.getByText("Nothing found")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /zzz/ }));
    expect(onSearch).toHaveBeenCalledWith("zzz");
  });

  it("leaves out the empty channels line while conversations have rows", () => {
    const { rerender } = render(<ChatSidebar {...withDms({ channels: [] })} />);
    expect(screen.queryByText(en.page.noChannelsTitle)).toBeNull();
    rerender(<ChatSidebar {...props({ channels: [] })} />);
    expect(screen.getByText(en.page.noChannelsTitle)).toBeTruthy();
  });

  it("shows typing on a conversation row in place of the preview, never as the caller", () => {
    const view = withDms().conversations!;
    const typingOf = (id: string) =>
      id === "d-thao" ? ["thao"] : id === "d-quan" ? ["quan", "thao"] : [];
    render(
      <ChatSidebar {...withDms({ conversations: { ...view, typingOf } })} />,
    );
    const rows = screen.getAllByRole("button", { name: /typing/ });
    expect(rows).toHaveLength(2);
    expect(rows[0]!.textContent).toContain("typing");
    expect(rows[0]!.querySelectorAll(".chat-typing-wave")).toHaveLength(3);
  });

  it("names the typer in a group and counts two or more", () => {
    const group = {
      ...dm("g-1", "thao"),
      kind: "group" as const,
      name: "Nhóm",
    };
    const view = withDms().conversations!;
    for (const [ids, text] of [
      [["thao"], "Nguyễn Thảo is typing"],
      [["thao", "quan"], "2 people are typing"],
    ] as const) {
      const { unmount } = render(
        <ChatSidebar
          {...withDms({
            conversations: {
              ...view,
              conversations: [group],
              typingOf: () => ids,
            },
          })}
        />,
      );
      expect(screen.getByText(text)).toBeTruthy();
      unmount();
    }
  });

  it("has no account footer: the caller lives only in the app rail", () => {
    render(<ChatSidebar {...props()} />);
    const nav = screen.getByRole("navigation", { name: en.sidebar.navLabel });
    expect(screen.queryByText("Nguyễn Thu Hà")).toBeNull();
    expect(nav.querySelector("footer")).toBeNull();
    expect(nav.querySelector("[data-slot=avatar]")).toBeNull();
    // The last block of the column is the channel list, never an account strip.
    expect(nav.lastElementChild?.querySelector("ul")).not.toBeNull();
  });
});

describe("ChatSidebar mark all as read", () => {
  it("offers the button only while something is unread, calls once and is disabled while pending", () => {
    const onMarkAllRead = vi.fn();
    const quiet = render(
      <ChatSidebar
        {...props({
          unreadBySpace: new Map([["c-general", { count: 0, muted: false }]]),
          onMarkAllRead,
        })}
      />,
    );
    expect(
      screen.queryByRole("button", { name: "Mark all as read" }),
    ).toBeNull();
    quiet.unmount();

    const mutedOnly = render(
      <ChatSidebar
        {...props({
          unreadBySpace: new Map([["c-general", { count: 5, muted: true }]]),
          onMarkAllRead,
        })}
      />,
    );
    expect(
      screen.queryByRole("button", { name: "Mark all as read" }),
    ).toBeNull();
    mutedOnly.unmount();

    const busy = render(<ChatSidebar {...props({ onMarkAllRead })} />);
    fireEvent.click(screen.getByRole("button", { name: "Mark all as read" }));
    expect(onMarkAllRead).toHaveBeenCalledTimes(1);
    busy.rerender(
      <ChatSidebar {...props({ onMarkAllRead, markingAllRead: true })} />,
    );
    expect(
      (
        screen.getByRole("button", {
          name: "Mark all as read",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("is absent without a handler", () => {
    render(<ChatSidebar {...props()} />);
    expect(
      screen.queryByRole("button", { name: "Mark all as read" }),
    ).toBeNull();
  });
});

describe("groupChannels", () => {
  it("keeps the ungrouped channels first and each group as one run, in order", () => {
    const groups = groupChannels([
      channel("a", "a", { groupId: "g-1" }),
      channel("b", "b"),
      channel("c", "c", { groupId: "g-1" }),
      channel("d", "d", { groupId: "g-2" }),
    ]);
    expect(groups.map((group) => group.groupId)).toEqual([null, "g-1", "g-2"]);
    expect(groups[1]!.channels.map((entry) => entry.id)).toEqual(["a", "c"]);
  });
});
