import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import {
  ChatSidebar,
  groupChannels,
  unreadBadge,
  type ChatSidebarProps,
} from "../web/components/chat-sidebar";
import type { ChatChannel } from "../core/types/chat";
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
  it("marks the open channel as the current page and names unread counts for a screen reader", () => {
    render(<ChatSidebar {...props()} />);
    const open = screen.getByRole("button", { name: /general/ });
    expect(open.getAttribute("aria-current")).toBe("page");
    expect(open.getAttribute("aria-label") ?? "").toContain(
      "120 unread messages",
    );
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

  it("shows the jump-to field as plain text, not as a control", () => {
    render(<ChatSidebar {...props()} />);
    expect(
      screen.queryByRole("button", { name: /Jump to or search/ }),
    ).toBeNull();
    expect(
      screen.getByText("Jump to or search").closest("[aria-hidden='true']"),
    ).not.toBeNull();
  });

  it("has no account footer: the caller lives only in the app rail", () => {
    render(<ChatSidebar {...props()} />);
    expect(screen.queryByText("Nguyễn Thu Hà")).toBeNull();
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
