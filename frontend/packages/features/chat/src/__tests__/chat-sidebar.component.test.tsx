import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@notrelix/testing";
import {
  ChatSidebar,
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
});
