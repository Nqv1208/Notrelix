import {
  act,
  fireEvent,
  render,
  renderHook,
  screen,
  within,
} from "@notrelix/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ComponentProps } from "react";

// The real DndContext is kept; the test only reads the handlers it was given, so a drag can be played without layout.
const dnd = vi.hoisted(() => ({
  props: null as null | {
    onDragMove?: (event: unknown) => void;
    onDragEnd?: (event: unknown) => void;
    onDragStart?: (event: unknown) => void;
  },
}));
vi.mock("@dnd-kit/core", async (importOriginal) => {
  const original = await importOriginal<typeof import("@dnd-kit/core")>();
  return {
    ...original,
    DndContext: (props: ComponentProps<typeof original.DndContext>) => {
      dnd.props = props as never;
      return <original.DndContext {...props} />;
    },
  };
});

import {
  ChatSidebar,
  type ChatSidebarProps,
} from "../web/components/chat-sidebar";
import {
  collapseKey,
  useCollapsedGroups,
} from "../web/components/use-collapsed-groups";
import type { ChatChannel, ChatChannelGroup } from "../core/types/chat";
import { en } from "../web/components/chat.copy";

function channel(
  id: string,
  groupId: string | null,
  position: number,
  overrides: Partial<ChatChannel> = {},
): ChatChannel {
  return {
    id,
    name: id,
    topic: null,
    kind: "channel_public",
    groupId,
    position,
    version: 3,
    isMember: true,
    isArchived: false,
    role: "manager",
    notificationLevel: "all",
    ...overrides,
  } as ChatChannel;
}

const GROUPS: ChatChannelGroup[] = [
  { id: "g-team", name: "Team", position: 1, version: 2 },
  { id: "g-empty", name: "Archive board", position: 2, version: 1 },
];

function props(overrides: Partial<ChatSidebarProps> = {}): ChatSidebarProps {
  return {
    channels: [
      channel("general", null, 1),
      channel("random", null, 2),
      channel("design", "g-team", 1),
      channel("eng", "g-team", 2),
    ],
    groups: GROUPS,
    status: "ready",
    activeChannelId: "general",
    unreadBySpace: new Map(),
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

function openMenu(name: string) {
  fireEvent.keyDown(screen.getByRole("button", { name }), { key: "Enter" });
}

/** The channel options button in the row of one channel. */
function channelMenuOf(channelName: string): HTMLElement | null {
  const row = screen
    .getByRole("button", { name: new RegExp(": " + channelName + "\\b") })
    .closest("li");
  return row
    ? within(row).queryByRole("button", { name: "Channel options" })
    : null;
}

function openChannelMenu(channelName: string) {
  fireEvent.keyDown(channelMenuOf(channelName)!, { key: "Enter" });
}

beforeEach(() => {
  dnd.props = null;
});

describe("Sidebar categories (UC-CHT-A02)", () => {
  it("shows an empty category as a heading with a line that says so", () => {
    render(<ChatSidebar {...props()} />);
    expect(
      screen.getByRole("heading", { name: /Archive board/ }),
    ).not.toBeNull();
    expect(screen.getByText(en.groups.empty)).not.toBeNull();
  });

  it("tells a person who can move channels to drag one into an empty category", () => {
    render(<ChatSidebar {...props({ canReorderChannel: () => true })} />);
    expect(screen.getByText(en.groups.emptyManage)).not.toBeNull();
  });

  it("offers no category control to a person who may not manage categories", () => {
    render(<ChatSidebar {...props()} />);
    expect(
      screen.queryByRole("button", { name: en.groups.newCategory }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: /Options for category/ }),
    ).toBeNull();
  });

  it("offers New category next to New channel and opens the dialog through its handler", () => {
    const onCreateCategory = vi.fn();
    render(<ChatSidebar {...props({ onCreateCategory })} />);
    fireEvent.click(
      screen.getByRole("button", { name: en.groups.newCategory }),
    );
    expect(onCreateCategory).toHaveBeenCalledOnce();
  });

  it("renames and deletes from the menu on the category heading", async () => {
    const onRename = vi.fn();
    const onDelete = vi.fn();
    render(<ChatSidebar {...props({ category: { onRename, onDelete } })} />);
    openMenu("Options for category Team");
    fireEvent.click(
      await screen.findByRole("menuitem", { name: en.groups.rename }),
    );
    expect(onRename).toHaveBeenCalledWith(GROUPS[0]);

    openMenu("Options for category Team");
    fireEvent.click(
      await screen.findByRole("menuitem", { name: en.groups.delete }),
    );
    expect(onDelete).toHaveBeenCalledWith(GROUPS[0]);
  });
});

describe("Sidebar order (UC-CHT-A04)", () => {
  it("shows no drag or move entry on a channel the person does not manage", () => {
    render(
      <ChatSidebar
        {...props({ canReorderChannel: (c) => c.id !== "design" })}
      />,
    );
    expect(channelMenuOf("eng")).not.toBeNull();
    expect(channelMenuOf("design")).toBeNull();
  });

  it("moves a channel down with the keyboard menu, to the midpoint-free end of its run", async () => {
    const onReorderChannels = vi.fn();
    render(
      <ChatSidebar
        {...props({ canReorderChannel: () => true, onReorderChannels })}
      />,
    );
    openChannelMenu("design");
    fireEvent.click(
      await screen.findByRole("menuitem", { name: en.groups.channelMoveDown }),
    );
    expect(onReorderChannels).toHaveBeenCalledWith([
      {
        channelId: "design",
        groupId: "g-team",
        position: 2 + 1024,
        expectedVersion: 3,
      },
    ]);
    // A screen reader hears the move.
    expect(screen.getByText("Moved #design down.")).not.toBeNull();
  });

  it("moves the first channel of a category up into the end of the category above", async () => {
    const onReorderChannels = vi.fn();
    render(
      <ChatSidebar
        {...props({ canReorderChannel: () => true, onReorderChannels })}
      />,
    );
    openChannelMenu("design");
    fireEvent.click(
      await screen.findByRole("menuitem", { name: en.groups.channelMoveUp }),
    );
    expect(onReorderChannels).toHaveBeenCalledWith([
      {
        channelId: "design",
        groupId: null,
        position: 2 + 1024,
        expectedVersion: 3,
      },
    ]);
  });

  it("disables Move up on the very first channel", async () => {
    render(<ChatSidebar {...props({ canReorderChannel: () => true })} />);
    openChannelMenu("general");
    const up = await screen.findByRole("menuitem", {
      name: en.groups.channelMoveUp,
    });
    expect(up.getAttribute("aria-disabled")).toBe("true");
  });

  it("moves a category with its menu, between the neighbours", async () => {
    const onReorderGroups = vi.fn();
    render(
      <ChatSidebar
        {...props({
          category: { onRename: vi.fn(), onDelete: vi.fn() },
          onReorderGroups,
        })}
      />,
    );
    openMenu("Options for category Archive board");
    fireEvent.click(
      await screen.findByRole("menuitem", { name: en.groups.moveUp }),
    );
    expect(onReorderGroups).toHaveBeenCalledWith([
      { group: GROUPS[1], position: 1 - 1024 },
    ]);
  });

  it("drops a dragged channel between two rows at the midpoint and shows the drop line while over it", () => {
    const onReorderChannels = vi.fn();
    render(
      <ChatSidebar
        {...props({ canReorderChannel: () => true, onReorderChannels })}
      />,
    );
    const event = {
      active: {
        data: { current: { type: "channel", channelId: "general" } },
        rect: { current: { translated: { top: 80, height: 32 } } },
      },
      over: {
        data: {
          current: { type: "channel", channelId: "eng", groupId: "g-team" },
        },
        rect: { top: 90, height: 32 },
      },
    };
    act(() => dnd.props!.onDragStart!(event));
    act(() => dnd.props!.onDragMove!(event));
    expect(screen.getByTestId("chat-drop-line")).not.toBeNull();

    act(() => dnd.props!.onDragEnd!(event));
    // Over the upper half of eng: before it, between design (1) and eng (2).
    expect(onReorderChannels).toHaveBeenCalledWith([
      {
        channelId: "general",
        groupId: "g-team",
        position: 1.5,
        expectedVersion: 3,
      },
    ]);
    expect(screen.queryByTestId("chat-drop-line")).toBeNull();
  });

  it("drops onto the heading of an empty category to put the channel there", () => {
    const onReorderChannels = vi.fn();
    render(
      <ChatSidebar
        {...props({ canReorderChannel: () => true, onReorderChannels })}
      />,
    );
    const event = {
      active: {
        data: { current: { type: "channel", channelId: "random" } },
        rect: { current: { translated: null } },
      },
      over: {
        data: { current: { type: "section", groupId: "g-empty" } },
        rect: { top: 0, height: 60 },
      },
    };
    act(() => dnd.props!.onDragEnd!(event));
    expect(onReorderChannels).toHaveBeenCalledWith([
      {
        channelId: "random",
        groupId: "g-empty",
        position: 0,
        expectedVersion: 3,
      },
    ]);
  });

  it("changes nothing when a channel is dropped where it already is", () => {
    const onReorderChannels = vi.fn();
    render(
      <ChatSidebar
        {...props({ canReorderChannel: () => true, onReorderChannels })}
      />,
    );
    act(() =>
      dnd.props!.onDragEnd!({
        active: {
          data: { current: { type: "channel", channelId: "general" } },
          rect: { current: { translated: { top: 0, height: 32 } } },
        },
        over: {
          data: {
            current: { type: "channel", channelId: "general", groupId: null },
          },
          rect: { top: 0, height: 32 },
        },
      }),
    );
    expect(onReorderChannels).not.toHaveBeenCalled();
  });

  it("drags a category over another to reorder categories", () => {
    const onReorderGroups = vi.fn();
    render(
      <ChatSidebar
        {...props({
          category: { onRename: vi.fn(), onDelete: vi.fn() },
          onReorderGroups,
        })}
      />,
    );
    act(() =>
      dnd.props!.onDragEnd!({
        active: {
          data: { current: { type: "group", groupId: "g-empty" } },
          rect: { current: { translated: { top: 10, height: 30 } } },
        },
        over: {
          data: { current: { type: "section", groupId: "g-team" } },
          rect: { top: 0, height: 100 },
        },
      }),
    );
    expect(onReorderGroups).toHaveBeenCalledWith([
      { group: GROUPS[1], position: 1 - 1024 },
    ]);
  });
});

describe("Collapse of a category (UC-CHT-A04)", () => {
  it("collapses a category for the reader: its rows leave, and the heading says so", () => {
    const onToggleCollapse = vi.fn();
    const { rerender } = render(
      <ChatSidebar
        {...props({ onToggleCollapse, isCollapsed: () => false })}
      />,
    );
    const toggle = screen.getByRole("button", { name: "Collapse Team" });
    expect(toggle.getAttribute("aria-expanded")).toBe("true");
    fireEvent.click(toggle);
    expect(onToggleCollapse).toHaveBeenCalledWith("g-team");

    rerender(
      <ChatSidebar
        {...props({
          onToggleCollapse,
          isCollapsed: (id) => id === "g-team",
        })}
      />,
    );
    expect(screen.queryByRole("button", { name: /design/ })).toBeNull();
    expect(
      screen
        .getByRole("button", { name: "Expand Team" })
        .getAttribute("aria-expanded"),
    ).toBe("false");
  });

  it("marks a collapsed category that holds an unread channel", () => {
    render(
      <ChatSidebar
        {...props({
          isCollapsed: (id) => id === "g-team",
          unreadBySpace: new Map([["design", { count: 4, muted: false }]]),
        })}
      />,
    );
    expect(screen.getByTestId("chat-group-unread-dot")).not.toBeNull();
  });
});

function memoryStore() {
  const data = new Map<string, string>();
  return {
    data,
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => void data.set(key, value),
  };
}

describe("useCollapsedGroups", () => {
  it("keeps the choice per Workspace and category in the host's storage, across a reload", () => {
    const store = memoryStore();
    const first = renderHook(() => useCollapsedGroups("ws-1", store));
    expect(first.result.current.isCollapsed("g1")).toBe(false);
    act(() => first.result.current.toggle("g1"));
    expect(first.result.current.isCollapsed("g1")).toBe(true);
    expect(store.data.get(collapseKey("ws-1", "g1"))).toBe("1");
    first.unmount();

    // A reload reads it back, for that Workspace only.
    const again = renderHook(() => useCollapsedGroups("ws-1", store));
    expect(again.result.current.isCollapsed("g1")).toBe(true);
    expect(again.result.current.isCollapsed("g2")).toBe(false);
    const other = renderHook(() => useCollapsedGroups("ws-2", store));
    expect(other.result.current.isCollapsed("g1")).toBe(false);

    act(() => again.result.current.toggle("g1"));
    expect(again.result.current.isCollapsed("g1")).toBe(false);
    expect(store.data.get(collapseKey("ws-1", "g1"))).toBe("0");
  });

  it("renders open and still toggles for the page when storage is blocked or missing", () => {
    const blocked = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    for (const store of [blocked, undefined]) {
      const { result, unmount } = renderHook(() =>
        useCollapsedGroups("ws-1", store),
      );
      expect(result.current.isCollapsed("g1")).toBe(false);
      act(() => result.current.toggle("g1"));
      expect(result.current.isCollapsed("g1")).toBe(true);
      unmount();
    }
  });
});
